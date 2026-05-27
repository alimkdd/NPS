using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Application.Interfaces;

public interface ISubscriptionReadRepository
{
    Task<SubscriptionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SubscriptionResponse> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        int? subscriberTypeId,
        int? communicationPreferenceId,
        int? interestId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SubscriptionStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
}
