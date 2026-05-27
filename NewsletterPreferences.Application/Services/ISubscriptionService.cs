using NewsletterPreferences.Application.Common;
using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Application.Services;

public interface ISubscriptionService
{
    Task<Result<UpsertSubscriptionResult>> UpsertAsync(UpsertSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result<SubscriptionResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<SubscriptionResponse>>> GetPagedAsync(SubscriptionFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> UnsubscribeAsync(UnsubscribeRequest request, CancellationToken cancellationToken = default);
}
