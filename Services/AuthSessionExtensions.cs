namespace CITWEB.Services;

public static class AuthSessionExtensions
{
    public const string RoleAdmin = "Admin";
    public const string RoleUser = "User";

    public static bool IsLoggedIn(this ISession session) =>
        !string.IsNullOrEmpty(session.GetString(AuthSessionKeys.UserEmail));

    public static string? GetUserEmail(this ISession session) =>
        session.GetString(AuthSessionKeys.UserEmail);

    public static string? GetUserName(this ISession session) =>
        session.GetString(AuthSessionKeys.UserName);

    public static string GetUserRole(this ISession session) =>
        session.GetString(AuthSessionKeys.UserRole) ?? RoleUser;

    public static bool IsAdmin(this ISession session) =>
        string.Equals(session.GetUserRole(), RoleAdmin, StringComparison.OrdinalIgnoreCase);

    public static void SetUser(this ISession session, string email, string displayName, string role)
    {
        session.SetString(AuthSessionKeys.UserEmail, email.Trim());
        session.SetString(AuthSessionKeys.UserName, displayName.Trim());
        session.SetString(AuthSessionKeys.UserRole, string.IsNullOrWhiteSpace(role) ? RoleUser : role.Trim());
    }

    public static void ClearUser(this ISession session)
    {
        session.Remove(AuthSessionKeys.UserEmail);
        session.Remove(AuthSessionKeys.UserName);
        session.Remove(AuthSessionKeys.UserRole);
    }
}
