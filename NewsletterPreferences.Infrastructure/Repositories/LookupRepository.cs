using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure.Persistence;

namespace NewsletterPreferences.Infrastructure.Repositories;

public class LookupRepository(AppDbContext context, IMemoryCache cache) : ILookupRepository
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);

    private const string SubscriberTypesKey = "lookup:subscriber-types";
    private const string CommunicationPreferencesKey = "lookup:communication-preferences";
    private const string NewsletterInterestsKey = "lookup:newsletter-interests";

    public Task<IReadOnlyList<SubscriberType>> GetSubscriberTypesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(SubscriberTypesKey, ct =>
            context.SubscriberTypes.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct),
            cancellationToken);

    public Task<IReadOnlyList<CommunicationPreference>> GetCommunicationPreferencesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(CommunicationPreferencesKey, ct =>
            context.CommunicationPreferences.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct),
            cancellationToken);

    public Task<IReadOnlyList<NewsletterInterest>> GetNewsletterInterestsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(NewsletterInterestsKey, ct =>
            context.NewsletterInterests.AsNoTracking().OrderBy(n => n.Id).ToListAsync(ct),
            cancellationToken);

    public async Task<bool> SubscriberTypeExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        var types = await GetSubscriberTypesAsync(cancellationToken);
        return types.Any(t => t.Id == id);
    }

    public async Task<bool> AllCommunicationPreferencesExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var prefs = await GetCommunicationPreferencesAsync(cancellationToken);
        var known = prefs.Select(p => p.Id).ToHashSet();
        return ids.Distinct().All(known.Contains);
    }

    public async Task<bool> AllInterestsExistAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var interests = await GetNewsletterInterestsAsync(cancellationToken);
        var known = interests.Select(i => i.Id).ToHashSet();
        return ids.Distinct().All(known.Contains);
    }

    private async Task<IReadOnlyList<T>> GetOrLoadAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<List<T>>> loader,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached) && cached is not null)
            return cached;

        var loaded = await loader(cancellationToken);
        IReadOnlyList<T> result = loaded;

        cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
            Size = 1
        });

        return result;
    }
}
