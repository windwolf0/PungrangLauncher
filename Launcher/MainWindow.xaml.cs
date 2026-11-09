using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using Microsoft.Identity.Client;
using PungrangLauncher.Configuration;
using PungrangLauncher.Models;
using PungrangLauncher.Services;

namespace PungrangLauncher;

public partial class MainWindow : Window
{
    private const string ApiBaseUrl = "https://windwolf0.github.io/PungrangLauncher/";
    private const string ServerHost = "windwolf0.kro.kr";
    private const int ServerPort = 25565;

    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly MicrosoftAccountService _accountService = new();
    private MinecraftAccountInfo? _currentAccount;
    private string? _verificationUrl;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAnnouncementsAsync();
        await RestoreCachedAccountAsync();
        await RefreshServerAsync();
    }

    private async Task LoadAnnouncementsAsync()
    {
        try
        {
            var api = new LauncherApiClient(_http);
            AnnouncementsList.ItemsSource = await api.GetAnnouncementsAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = "공지사항을 불러오지 못했습니다: " + ex.Message;
        }
    }

    private async Task RestoreCachedAccountAsync()
    {
        if (!MicrosoftAuthConfiguration.IsConfigured)
        {
            AccountStateText.Text = "앱 등록 필요";
            AccountNameText.Text = "Microsoft 계정 로그인은 앱 등록 Client ID 설정 후 사용할 수 있습니다.";
            AccountHelpText.Text =
                "docs/MICROSOFT_ACCOUNT_SETUP.md 안내에 따라 앱을 등록하고 " +
                "PUNGRANG_MICROSOFT_CLIENT_ID 환경 변수에 Client ID를 설정해 주세요.";
            MicrosoftLoginButton.IsEnabled = false;
            LogoutButton.IsEnabled = false;
            return;
        }

        MicrosoftLoginButton.IsEnabled = true;
        AccountStateText.Text = "저장된 계정 확인 중";
        AccountNameText.Text = "Microsoft 계정 연결 상태를 확인하고 있습니다.";

        try
        {
            MinecraftAccountInfo? account = await _accountService.TryRestoreLastAccountAsync();
            if (account is null)
            {
                SetLoggedOutState("로그인하면 Minecraft Java Edition 권한까지 확인합니다.");
                return;
            }

            SetLoggedInState(account);
        }
        catch (Exception ex)
        {
            _currentAccount = null;
            LogoutButton.IsEnabled = true;
            AccountStateText.Text = "다시 로그인 필요";
            AccountNameText.Text = "저장된 계정을 확인하지 못했습니다.";
            AccountHelpText.Text = ex.Message;
            StatusText.Text = "계정 자동 확인이 완료되지 않았습니다.";
        }
    }

    private async void MicrosoftLogin_Click(object sender, RoutedEventArgs e)
    {
        if (!MicrosoftAuthConfiguration.IsConfigured)
        {
            MessageBox.Show(
                "Microsoft 앱 등록 Client ID가 아직 설정되지 않았습니다. 개발 설정 문서를 확인해 주세요.",
                "Microsoft 로그인 설정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        MicrosoftLoginButton.IsEnabled = false;
        LogoutButton.IsEnabled = false;
        DeviceCodePanel.Visibility = Visibility.Collapsed;
        AccountStateText.Text = "인증 코드 발급 중";
        AccountNameText.Text = "Microsoft 인증을 준비하고 있습니다.";
        AccountHelpText.Text = "브라우저에서 Microsoft 계정 인증을 완료해 주세요.";
        StatusText.Text = "Microsoft 계정 인증 진행 중...";

        try
        {
            MinecraftAccountInfo account = await _accountService.SignInAsync(
                async deviceCode =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        _verificationUrl = deviceCode.VerificationUrl.ToString();
                        DeviceCodeText.Text = deviceCode.UserCode;
                        DeviceCodeUrlText.Text = _verificationUrl;
                        DeviceCodePanel.Visibility = Visibility.Visible;
                        AccountStateText.Text = "브라우저 인증 대기 중";
                        AccountNameText.Text = "인증 페이지에서 코드를 입력해 주세요.";
                        StatusText.Text = "Microsoft 계정 인증을 기다리고 있습니다.";
                    });

                    try
                    {
                        Process.Start(new ProcessStartInfo(_verificationUrl!)
                        {
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // The user can still open the displayed verification URL manually.
                    }
                });

            SetLoggedInState(account);
            StatusText.Text = "Microsoft, Xbox Live 및 Minecraft 계정 확인 완료";
        }
        catch (OperationCanceledException)
        {
            SetLoggedOutState("로그인이 취소되었습니다. 다시 시도할 수 있습니다.");
            StatusText.Text = "Microsoft 계정 로그인이 취소되었습니다.";
        }
        catch (MicrosoftAccountException ex)
        {
            SetLoggedOutState(ex.Message);
            StatusText.Text = "계정 인증을 완료하지 못했습니다.";
        }
        catch (MsalException ex)
        {
            SetLoggedOutState("Microsoft 인증에 실패했습니다: " + ex.Message);
            StatusText.Text = "Microsoft 인증에 실패했습니다.";
        }
        catch (HttpRequestException)
        {
            SetLoggedOutState("인증 서버에 연결하지 못했습니다. 네트워크 연결을 확인해 주세요.");
            StatusText.Text = "계정 인증 서버 연결 실패";
        }
        catch (Exception ex)
        {
            SetLoggedOutState("로그인 처리 중 오류가 발생했습니다: " + ex.Message);
            StatusText.Text = "계정 로그인 처리 오류";
        }
        finally
        {
            MicrosoftLoginButton.IsEnabled = MicrosoftAuthConfiguration.IsConfigured;
            LogoutButton.IsEnabled = _currentAccount is not null;
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        LogoutButton.IsEnabled = false;
        try
        {
            await _accountService.SignOutAsync();
            _currentAccount = null;
            SetLoggedOutState("Microsoft 계정 연결이 해제되었습니다.");
            StatusText.Text = "로그아웃 완료";
        }
        catch (Exception ex)
        {
            AccountHelpText.Text = "로그아웃을 완료하지 못했습니다: " + ex.Message;
            LogoutButton.IsEnabled = _currentAccount is not null;
        }
    }

    private void CopyDeviceCode_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DeviceCodeText.Text))
            return;

        try
        {
            Clipboard.SetText(DeviceCodeText.Text);
            StatusText.Text = "인증 코드를 클립보드에 복사했습니다.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "인증 코드 복사 실패: " + ex.Message;
        }
    }

    private void OpenVerificationPage_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_verificationUrl))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(_verificationUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusText.Text = "브라우저를 열지 못했습니다: " + ex.Message;
        }
    }

    private void SetLoggedInState(MinecraftAccountInfo account)
    {
        _currentAccount = account;
        AccountStateText.Text = "로그인 완료 · Java Edition 확인";
        AccountNameText.Text =
            $"Minecraft: {account.MinecraftName}  ·  Xbox: {account.Gamertag}";
        AccountHelpText.Text = "Minecraft Java Edition 이용 권한이 확인되었습니다.";
        DeviceCodePanel.Visibility = Visibility.Collapsed;
        LogoutButton.IsEnabled = true;
    }

    private void SetLoggedOutState(string message)
    {
        _currentAccount = null;
        AccountStateText.Text = "로그인되지 않음";
        AccountNameText.Text = "Minecraft 계정이 연결되지 않았습니다.";
        AccountHelpText.Text = message;
        DeviceCodePanel.Visibility = Visibility.Collapsed;
        LogoutButton.IsEnabled = false;
    }

    private async void RefreshServer_Click(object sender, RoutedEventArgs e)
    {
        await RefreshServerAsync();
    }

    private async Task RefreshServerAsync()
    {
        ServerStateText.Text = "확인 중...";
        try
        {
            var info = await new MinecraftStatusClient().QueryAsync(
                ServerHost, ServerPort);
            ServerStateText.Text = "온라인";
            PlayerCountText.Text = $"{info.OnlinePlayers} / {info.MaxPlayers}명 접속";
            MotdText.Text = info.Motd;
        }
        catch
        {
            ServerStateText.Text = "확인 불가";
            PlayerCountText.Text = "서버 응답을 받지 못했습니다.";
            MotdText.Text = "서버 주소, 포트 또는 상태 조회 설정을 확인해 주세요.";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _accountService.Dispose();
        _http.Dispose();
        base.OnClosed(e);
    }
}
