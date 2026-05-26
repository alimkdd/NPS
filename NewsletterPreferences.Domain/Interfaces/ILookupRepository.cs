using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Domain.Interfaces;

public interface ILookupRepository
{
    Task<IReadOnlyList<SubscriberType>> GetSubscriberTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunicationPreference>> GetCommunicationPreferencesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NewsletterInterest>> GetNewsletterInterestsAsync(CancellationToken cancellationToken = default);
    Task<bool> SubscriberTypeExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AllCommunicationPreferencesExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<bool> AllInterestsExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
