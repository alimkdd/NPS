using Microsoft.EntityFrameworkCore;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure.Persistence;

namespace NewsletterPreferences.Infrastructure.Repositories;

public class AdminUserRepository(AppDbContext context) : IAdminUserRepository
{
    public Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return context.AdminUsers
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.Username == normalized, cancellationToken);
    }

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.AdminUsers
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<AdminUser?> GetByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default)
    {
        // Look up the credential first, then load its admin with all credentials.
        var credential = await context.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);

        if (credential is null) return null;

        return await context.AdminUsers
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.Id == credential.AdminUserId, cancellationToken);
    }

    public Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken = default) =>
        context.AdminUsers.AddAsync(adminUser, cancellationToken).AsTask();

    public Task AddCredentialAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default) =>
        context.WebAuthnCredentials.AddAsync(credential, cancellationToken).AsTask();

    public void Update(AdminUser adminUser) => context.AdminUsers.Update(adminUser);
}
