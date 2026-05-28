using NewsletterPreferences.Domain.Common;

namespace NewsletterPreferences.Domain.Entities;

public class WebAuthnCredential : Entity
{
    public Guid AdminUserId { get; private set; }
    public byte[] CredentialId { get; private set; } = [];
    public byte[] PublicKey { get; private set; } = [];
    public uint SignCount { get; private set; }
    public Guid AaGuid { get; private set; }
    public string? FriendlyName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    public AdminUser AdminUser { get; private set; } = null!;

    private WebAuthnCredential() { }

    internal static WebAuthnCredential Create(
        Guid adminUserId,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaGuid,
        string? friendlyName)
    {
        if (credentialId is null || credentialId.Length == 0)
            throw new ArgumentException("CredentialId is required.", nameof(credentialId));
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("PublicKey is required.", nameof(publicKey));

        return new WebAuthnCredential
        {
            AdminUserId = adminUserId,
            CredentialId = credentialId,
            PublicKey = publicKey,
            SignCount = signCount,
            AaGuid = aaGuid,
            FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void RecordSuccessfulAssertion(uint newSignCount)
    {
        SignCount = newSignCount;
        LastUsedAt = DateTime.UtcNow;
    }
}
