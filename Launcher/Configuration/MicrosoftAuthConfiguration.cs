using System;

namespace PungrangLauncher.Configuration;

public static class MicrosoftAuthConfiguration
{
    // This is a public client ID, not a secret. Set it after registering the launcher
    // in Microsoft Entra. The environment variable is useful for local/CI builds.
    public static string ClientId =>
        Environment.GetEnvironmentVariable("PUNGRANG_MICROSOFT_CLIENT_ID")?.Trim() ?? "";

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
