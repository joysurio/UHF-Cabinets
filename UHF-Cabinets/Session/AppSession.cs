namespace UHFCabinets.Session;

public static class AppSession
{
    public static string? UserId { get; private set; }
    public static string? DisplayName { get; private set; }
    public static string? Role { get; private set; }
    public static bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public static void Set(string userId, string name, string? role)
    {
        UserId = userId;
        DisplayName = name;
        Role = role;
    }

    public static void Clear()
    {
        UserId = null;
        DisplayName = null;
        Role = null;
    }
}
