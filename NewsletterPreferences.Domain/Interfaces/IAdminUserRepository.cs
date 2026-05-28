using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Domain.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default);
    Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken = default);
    /// <summary>
    /// Explicitly attach a new credential as Added. Necessary because <see cref="WebAuthnCredential"/>
    /// inherits from <c>Entity</c>, which assigns a non-default Guid PK at construction — EF's
    /// auto change detection would otherwise treat it as an existing entity and emit UPDATE.
    /// </summary>
    Task AddCredentialAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default);
    void Update(AdminUser adminUser);
}
