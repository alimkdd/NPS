using Microsoft.EntityFrameworkCore;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure.Persistence;

namespace NewsletterPreferences.Infrastructure.Repositories;

public class LookupRepository(AppDbContext context) : ILookupRepository
{
    public async Task<IReadOnlyList<SubscriberType>> GetSubscriberTypesAsync(CancellationToken cancellationToken = default) =>
        await context.SubscriberTypes.OrderBy(s => s.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CommunicationPreference>> GetCommunicationPreferencesAsync(CancellationToken cancellationToken = default) =>
        await context.CommunicationPreferences.OrderBy(c => c.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NewsletterInterest>> GetNewsletterInterestsAsync(CancellationToken cancellationToken = default) =>
        await context.NewsletterInterests.OrderBy(n => n.Id).ToListAsync(cancellationToken);

    public async Task<bool> SubscriberTypeExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await context.SubscriberTypes.AnyAsync(s => s.Id == id, cancellationToken);

    public async Task<bool> AllCommunicationPreferencesExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        var count = await context.CommunicationPreferences.CountAsync(c => idList.Contains(c.Id), cancellationToken);
        return count == idList.Count;
    }

    public async Task<bool> AllInterestsExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        var count = await context.NewsletterInterests.CountAsync(n => idList.Contains(n.Id), cancellationToken);
        return count == idList.Count;
    }
}
