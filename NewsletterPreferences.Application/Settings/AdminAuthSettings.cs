namespace NewsletterPreferences.Application.Settings;

public class AdminAuthSettings
{
    public const string SectionName = "AdminAuth";
    public string Username { get; set; } = "admin";
    public string DisplayName { get; set; } = "Administrator";
}