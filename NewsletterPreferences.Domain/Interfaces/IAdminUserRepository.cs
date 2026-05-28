using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Domain.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default);
    Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken = default);
    void Update(AdminUser adminUser);
}
