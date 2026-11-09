using System.Net.Http;
using System.Windows;
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

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAnnouncementsAsync();
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
}
