using System.Net.Http;
using System.Net.Http.Json;
using PungrangLauncher.Models;

namespace PungrangLauncher.Services;

public sealed class LauncherApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<LauncherAnnouncement>> GetAnnouncementsAsync(
        CancellationToken cancellationToken = default)
    {
        return await http.GetFromJsonAsync<List<LauncherAnnouncement>>(
            "launcher/announcements.json", cancellationToken) ?? [];
    }
}
