using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Domain.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Subscription> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        int? subscriberTypeId,
        int? communicationPreferenceId,
        int? interestId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task DeleteAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
