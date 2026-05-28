namespace NewsletterPreferences.Application.Settings;

public class AdminAuthSettings
{
    public const string SectionName = "AdminAuth";

    /// <summary>
    /// Username of the bootstrapped admin (seeded on first startup).
    /// </summary>
    public string Username { get; set; } = "admin";
    public string DisplayName { get; set; } = "Administrator";
}
