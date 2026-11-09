using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Client;
using PungrangLauncher.Configuration;
using PungrangLauncher.Models;

namespace PungrangLauncher.Services;

public sealed class MicrosoftAccountService : IDisposable
{
    private const string XboxUserAuthUrl = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XboxXstsUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MinecraftLoginUrl = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string MinecraftEntitlementsUrl = "https://api.minecraftservices.com/entitlements/mcstore";
    private const string MinecraftProfileUrl = "https://api.minecraftservices.com/minecraft/profile";

    private static readonly byte[] CacheEntropy = Encoding.UTF8.GetBytes("PungrangLauncher.MSAL.Cache.v1");
    private static readonly JsonSerializerOptions XboxJsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(25)
    };

    private readonly object _applicationLock = new();
    private readonly object _cacheLock = new();
    private readonly string _cachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PungrangLauncher",
        "Authentication",
        "msal.cache");

    private IPublicClientApplication? _application;

    public bool IsConfigured => MicrosoftAuthConfiguration.IsConfigured;

    public async Task<MinecraftAccountInfo> SignInAsync(
        Func<DeviceCodeResult, Task> showDeviceCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(showDeviceCode);

        IPublicClientApplication application = GetApplication();
        AuthenticationResult microsoftResult = await application
            .AcquireTokenWithDeviceCode(MicrosoftAuthConfiguration.Scopes, showDeviceCode)
            .ExecuteAsync(cancellationToken);

        return await CompleteMinecraftSignInAsync(microsoftResult, cancellationToken);
    }

    public async Task<MinecraftAccountInfo?> TryRestoreLastAccountAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        IPublicClientApplication application = GetApplication();
        IEnumerable<IAccount> accounts = await application.GetAccountsAsync();

        foreach (IAccount account in accounts)
        {
            try
            {
                AuthenticationResult microsoftResult = await application
                    .AcquireTokenSilent(MicrosoftAuthConfiguration.Scopes, account)
                    .ExecuteAsync(cancellationToken);

                return await CompleteMinecraftSignInAsync(microsoftResult, cancellationToken);
            }
            catch (MsalUiRequiredException)
            {
                // The account still exists in the cache, but Microsoft requires an interactive sign-in.
            }
        }

        return null;
    }

    public async Task SignOutAsync()
    {
        if (_application is null)
            return;

        IEnumerable<IAccount> accounts = await _application.GetAccountsAsync();
        foreach (IAccount account in accounts)
            await _application.RemoveAsync(account);

        lock (_cacheLock)
        {
            if (File.Exists(_cachePath))
                File.Delete(_cachePath);
        }
    }

    private IPublicClientApplication GetApplication()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Microsoft 로그인 앱 등록이 아직 설정되지 않았습니다. " +
                "Microsoft Entra 앱 등록 후 PUNGRANG_MICROSOFT_CLIENT_ID 환경 변수에 Client ID를 설정해야 합니다.");
        }

        lock (_applicationLock)
        {
            if (_application is not null)
                return _application;

            _application = PublicClientApplicationBuilder
                .Create(MicrosoftAuthConfiguration.ClientId)
                .WithAuthority(MicrosoftAuthConfiguration.Authority)
                .Build();

            RegisterTokenCache(_application.UserTokenCache);
            return _application;
        }
    }

    private void RegisterTokenCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(args =>
        {
            lock (_cacheLock)
            {
                if (!File.Exists(_cachePath))
                    return;

                byte[]? clearBytes = null;
                try
                {
                    byte[] encryptedBytes = File.ReadAllBytes(_cachePath);
                    clearBytes = ProtectedData.Unprotect(
                        encryptedBytes,
                        CacheEntropy,
                        DataProtectionScope.CurrentUser);

                    args.TokenCache.DeserializeMsalV3(clearBytes);
                }
                catch (CryptographicException)
                {
                    // A cache created under another Windows account or an obsolete cache is not reusable.
                    TryDeleteCache();
                }
                catch (InvalidDataException)
                {
                    TryDeleteCache();
                }
                finally
                {
                    if (clearBytes is not null)
                        CryptographicOperations.ZeroMemory(clearBytes);
                }
            }
        });

        tokenCache.SetAfterAccess(args =>
        {
            if (!args.HasStateChanged)
                return;

            lock (_cacheLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!);

                byte[] clearBytes = args.TokenCache.SerializeMsalV3();
                byte[] encryptedBytes = ProtectedData.Protect(
                    clearBytes,
                    CacheEntropy,
                    DataProtectionScope.CurrentUser);

                string temporaryPath = _cachePath + ".tmp";
                try
                {
                    File.WriteAllBytes(temporaryPath, encryptedBytes);
                    File.Move(temporaryPath, _cachePath, overwrite: true);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(clearBytes);
                    CryptographicOperations.ZeroMemory(encryptedBytes);
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
            }
        });
    }

    private void TryDeleteCache()
    {
        try
        {
            if (File.Exists(_cachePath))
                File.Delete(_cachePath);
        }
        catch (IOException)
        {
            // A stale cache must never prevent the user from starting a fresh sign-in.
        }
        catch (UnauthorizedAccessException)
        {
            // The MSAL operation can continue without restoring this cache file.
        }
    }

    private async Task<MinecraftAccountInfo> CompleteMinecraftSignInAsync(
        AuthenticationResult microsoftResult,
        CancellationToken cancellationToken)
    {
        string microsoftAccessToken = microsoftResult.AccessToken;

        using JsonDocument xboxUser = await PostXboxJsonAsync(
            XboxUserAuthUrl,
            new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = $"d={microsoftAccessToken}"
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            },
            cancellationToken);

        string xboxUserToken = GetRequiredString(xboxUser.RootElement, "Token");
        JsonElement userClaims = xboxUser.RootElement
            .GetProperty("DisplayClaims")
            .GetProperty("xui")[0];

        string userHash = GetRequiredString(userClaims, "uhs");
        string gamertag = userClaims.TryGetProperty("gtg", out JsonElement gtg)
            ? gtg.GetString() ?? "Xbox 사용자"
            : "Xbox 사용자";

        using JsonDocument xsts = await PostXboxJsonAsync(
            XboxXstsUrl,
            new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { xboxUserToken }
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            },
            cancellationToken);

        string xstsToken = GetRequiredString(xsts.RootElement, "Token");
        string identityToken = $"XBL3.0 x={userHash};{xstsToken}";

        using JsonDocument minecraftLogin = await PostMinecraftJsonAsync(
            MinecraftLoginUrl,
            new { identityToken },
            cancellationToken);

        string minecraftAccessToken = GetRequiredString(
            minecraftLogin.RootElement,
            "access_token");

        await EnsureMinecraftOwnershipAsync(minecraftAccessToken, cancellationToken);

        using JsonDocument profile = await GetMinecraftJsonAsync(
            MinecraftProfileUrl,
            minecraftAccessToken,
            cancellationToken);

        string minecraftName = GetRequiredString(profile.RootElement, "name");
        string minecraftUuid = GetRequiredString(profile.RootElement, "id");

        return new MinecraftAccountInfo
        {
            MicrosoftUsername = microsoftResult.Account?.Username ?? "Microsoft 계정",
            Gamertag = gamertag,
            MinecraftName = minecraftName,
            MinecraftUuid = minecraftUuid,
            MinecraftAccessToken = minecraftAccessToken
        };
    }

    private async Task EnsureMinecraftOwnershipAsync(
        string minecraftAccessToken,
        CancellationToken cancellationToken)
    {
        using JsonDocument entitlements = await GetMinecraftJsonAsync(
            MinecraftEntitlementsUrl,
            minecraftAccessToken,
            cancellationToken);

        if (!entitlements.RootElement.TryGetProperty("items", out JsonElement items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            throw new MicrosoftAccountException(
                "Minecraft 이용 권한 응답을 확인할 수 없습니다. 잠시 후 다시 시도해 주세요.");
        }

        bool ownsJavaEdition = items.EnumerateArray().Any(item =>
            item.TryGetProperty("name", out JsonElement name) &&
            name.ValueKind == JsonValueKind.String &&
            (string.Equals(name.GetString(), "game_minecraft", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(name.GetString(), "product_minecraft", StringComparison.OrdinalIgnoreCase)));

        if (!ownsJavaEdition)
        {
            throw new MicrosoftAccountException(
                "이 Microsoft 계정에서 Minecraft Java Edition 이용 권한을 찾지 못했습니다. " +
                "구매 계정 또는 Game Pass 구독 계정을 확인해 주세요.");
        }
    }

    private async Task<JsonDocument> PostXboxJsonAsync(
        string url,
        object body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-xbl-contract-version", "1");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(body, options: XboxJsonOptions);

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new MicrosoftAccountException(MapXboxError(response.StatusCode, responseBody));

        return ParseJson(responseBody, "Xbox 인증 서버의 응답을 읽을 수 없습니다.");
    }

    private async Task<JsonDocument> PostMinecraftJsonAsync(
        string url,
        object body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new MicrosoftAccountException(
                "Minecraft Services가 앱 등록을 거부했습니다. " +
                "Microsoft 앱 등록의 Minecraft API 사용 권한 상태를 확인해야 합니다.");
        }

        if (!response.IsSuccessStatusCode)
            throw new MicrosoftAccountException(
                $"Minecraft 계정 인증에 실패했습니다. (HTTP {(int)response.StatusCode})");

        return ParseJson(responseBody, "Minecraft 인증 서버의 응답을 읽을 수 없습니다.");
    }

    private async Task<JsonDocument> GetMinecraftJsonAsync(
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound &&
            string.Equals(url, MinecraftProfileUrl, StringComparison.Ordinal))
        {
            throw new MicrosoftAccountException(
                "Minecraft Java Edition 프로필을 찾지 못했습니다. minecraft.net에서 프로필을 먼저 생성해 주세요.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new MicrosoftAccountException(
                "Minecraft Services 접근이 거부되었습니다. 계정 권한과 앱 등록 상태를 확인해 주세요.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new MicrosoftAccountException(
                $"Minecraft 계정 정보를 가져오지 못했습니다. (HTTP {(int)response.StatusCode})");
        }

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseJson(responseBody, "Minecraft 계정 정보 응답을 읽을 수 없습니다.");
    }

    private static JsonDocument ParseJson(string json, string errorMessage)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new MicrosoftAccountException(errorMessage);
        }
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new MicrosoftAccountException(
                $"인증 응답에 필수 정보({propertyName})가 없습니다.");
        }

        return property.GetString()!;
    }

    private static string MapXboxError(HttpStatusCode statusCode, string responseBody)
    {
        long? xerr = null;
        try
        {
            using JsonDocument error = JsonDocument.Parse(responseBody);
            if (error.RootElement.TryGetProperty("XErr", out JsonElement xerrElement) &&
                xerrElement.TryGetInt64(out long value))
            {
                xerr = value;
            }
        }
        catch (JsonException)
        {
            // Use the generic message when the service does not return JSON.
        }

        return xerr switch
        {
            2148916233 => "Xbox 프로필이 없습니다. xbox.com에서 Xbox 프로필을 먼저 만들어 주세요.",
            2148916235 => "현재 지역에서 Xbox Live 서비스를 사용할 수 없는 계정입니다.",
            2148916236 or 2148916237 =>
                "Xbox 계정의 성인 인증 또는 연령 확인이 필요합니다. Xbox 계정 설정을 확인해 주세요.",
            2148916238 =>
                "자녀 계정은 보호자 Microsoft 계정의 가족 구성원으로 등록해야 합니다.",
            _ => $"Xbox 계정 인증에 실패했습니다. (HTTP {(int)statusCode})"
        };
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}

public sealed class MicrosoftAccountException(string message) : Exception(message);
