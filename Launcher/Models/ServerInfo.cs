namespace PungrangLauncher.Models;

public sealed class ServerInfo
{
    public bool Online { get; set; }
    public int OnlinePlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string Motd { get; set; } = "";
    public DateTimeOffset CheckedAt { get; set; }
}
