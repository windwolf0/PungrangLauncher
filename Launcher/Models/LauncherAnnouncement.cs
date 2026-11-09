namespace PungrangLauncher.Models;

public sealed class LauncherAnnouncement
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; }
}
