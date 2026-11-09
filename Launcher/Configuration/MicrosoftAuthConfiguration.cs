using System;
using System.Reflection;

namespace PungrangLauncher.Configuration;

public static class MicrosoftAuthConfiguration
{
    // The client ID is a public identifier, not a secret. Prefer a build-time embedded
    // value for releases, while allowing a runtime environment override for developers.
    public static string ClientId
    {
        get
        {
            string? environmentValue = Environment
                .GetEnvironmentVariable("PUNGRANG_MICROSOFT_CLIENT_ID")?
                .Trim();

            if (!string.IsNullOrWhiteSpace(environmentValue))
                return environmentValue;

            return typeof(MicrosoftAuthConfiguration).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "MicrosoftClientId")?
                .Value?
                .Trim() ?? "";
        }
    }

    public static bool IsConfigured =>
        Guid.TryParse(ClientId, out Guid clientId) && clientId != Guid.Empty;

    public const string Authority = "https://login.microsoftonline.com/consumers";

    // Xbox Live sign-in and offline access are required for the Minecraft account exchange.
    public static readonly string[] Scopes =
    [
        "XboxLive.signin",
        "XboxLive.offline_access"
    ];
}
