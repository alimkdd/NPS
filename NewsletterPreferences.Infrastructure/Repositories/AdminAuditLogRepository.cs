using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure.Persistence;

namespace NewsletterPreferences.Infrastructure.Repositories;

public class AdminAuditLogRepository(AppDbContext context) : IAdminAuditLogRepository
{
    public async Task AddAsync(AdminAuditLog entry, CancellationToken cancellationToken = default)
    {
        await context.AdminAuditLogs.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
