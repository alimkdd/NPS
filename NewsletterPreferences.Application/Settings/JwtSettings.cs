namespace NewsletterPreferences.Application.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "NewsletterPreferences";
    public string Audience { get; set; } = "NewsletterPreferences.Admin";
    /// <summary>
    /// Symmetric HMAC-SHA256 signing key. Must be at least 32 bytes when UTF-8 decoded.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
