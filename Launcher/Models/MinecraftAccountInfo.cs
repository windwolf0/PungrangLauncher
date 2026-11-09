namespace PungrangLauncher.Models;

public sealed class MinecraftAccountInfo
{
    public required string MicrosoftUsername { get; init; }
    public required string Gamertag { get; init; }
    public required string MinecraftName { get; init; }
    public required string MinecraftUuid { get; init; }

    // Kept in memory for the upcoming game-launch integration. Never display or log this value.
    internal required string MinecraftAccessToken { get; init; }
}
