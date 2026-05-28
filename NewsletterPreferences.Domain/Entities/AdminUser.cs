using NewsletterPreferences.Domain.Common;

namespace NewsletterPreferences.Domain.Entities;

public class AdminUser : Entity
{
    private readonly List<WebAuthnCredential> _credentials = [];

    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<WebAuthnCredential> Credentials => _credentials.AsReadOnly();

    private AdminUser() { }

    public static AdminUser Create(string username, string displayName)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        return new AdminUser
        {
            Username = username.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }

    public WebAuthnCredential AddCredential(
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaGuid,
        string? friendlyName)
    {
        var credential = WebAuthnCredential.Create(Id, credentialId, publicKey, signCount, aaGuid, friendlyName);
        _credentials.Add(credential);
        return credential;
    }
}
