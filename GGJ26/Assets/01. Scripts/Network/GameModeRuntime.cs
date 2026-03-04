using System;

public static class GameModeRuntime
{
    public const string Classic = "classic";
    public const string Deathmatch = "deathmatch";

    private static string currentMode = Classic;

    public static string CurrentMode => currentMode;
    public static bool IsDeathmatch => string.Equals(currentMode, Deathmatch, StringComparison.OrdinalIgnoreCase);

    public static void SetMode(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            currentMode = Classic;
            return;
        }

        currentMode = string.Equals(mode, Deathmatch, StringComparison.OrdinalIgnoreCase) ? Deathmatch : Classic;
    }
}

