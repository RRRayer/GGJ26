using System;

public static class RoomSessionNameCodec
{
    private const string PasswordSeparator = "#";
    private const string ModeMarker = "::m=";

    public static string Encode(string roomName, string password, string mode)
    {
        string safeRoom = string.IsNullOrWhiteSpace(roomName) ? "room" : roomName.Trim();
        string safeMode = NormalizeMode(mode);
        string session = safeRoom;

        if (string.IsNullOrEmpty(password) == false)
        {
            session += PasswordSeparator + password;
        }

        session += ModeMarker + safeMode;
        return session;
    }

    public static string DecodeMode(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            return GameModeRuntime.Classic;
        }

        int marker = sessionName.LastIndexOf(ModeMarker, StringComparison.Ordinal);
        if (marker < 0)
        {
            return GameModeRuntime.Classic;
        }

        string mode = sessionName.Substring(marker + ModeMarker.Length).Trim();
        return NormalizeMode(mode);
    }

    public static string DecodeDisplayRoomName(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            return string.Empty;
        }

        string withoutMode = RemoveModeSuffix(sessionName);
        int separator = withoutMode.IndexOf(PasswordSeparator, StringComparison.Ordinal);
        if (separator >= 0)
        {
            return withoutMode.Substring(0, separator);
        }

        return withoutMode;
    }

    public static bool HasPassword(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            return false;
        }

        string withoutMode = RemoveModeSuffix(sessionName);
        int separator = withoutMode.IndexOf(PasswordSeparator, StringComparison.Ordinal);
        return separator >= 0 && separator < withoutMode.Length - 1;
    }

    public static bool MatchesRoomAndPassword(string sessionName, string roomName, string password)
    {
        string safeRoom = roomName == null ? string.Empty : roomName.Trim();
        string safePassword = password == null ? string.Empty : password.Trim();
        string withoutMode = RemoveModeSuffix(sessionName);

        int separator = withoutMode.IndexOf(PasswordSeparator, StringComparison.Ordinal);
        string baseRoom = separator >= 0 ? withoutMode.Substring(0, separator) : withoutMode;
        string pass = separator >= 0 && separator < withoutMode.Length - 1 ? withoutMode.Substring(separator + 1) : string.Empty;

        return string.Equals(baseRoom, safeRoom, StringComparison.Ordinal) &&
               string.Equals(pass, safePassword, StringComparison.Ordinal);
    }

    private static string RemoveModeSuffix(string sessionName)
    {
        int marker = sessionName.LastIndexOf(ModeMarker, StringComparison.Ordinal);
        if (marker < 0)
        {
            return sessionName;
        }

        return sessionName.Substring(0, marker);
    }

    private static string NormalizeMode(string mode)
    {
        return string.Equals(mode, GameModeRuntime.Deathmatch, StringComparison.OrdinalIgnoreCase)
            ? GameModeRuntime.Deathmatch
            : GameModeRuntime.Classic;
    }
}
