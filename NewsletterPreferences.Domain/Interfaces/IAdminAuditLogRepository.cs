using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Domain.Interfaces;

public interface IAdminAuditLogRepository
{
    Task AddAsync(AdminAuditLog entry, CancellationToken cancellationToken = default);
}
