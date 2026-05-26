using Microsoft.EntityFrameworkCore;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure.Persistence;

namespace NewsletterPreferences.Infrastructure.Repositories;

public class SubscriptionRepository(AppDbContext context) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Subscriptions
            .Include(s => s.SubscriberType)
            .Include(s => s.CommunicationPreferences).ThenInclude(scp => scp.CommunicationPreference)
            .Include(s => s.Interests).ThenInclude(si => si.NewsletterInterest)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Subscription?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Subscriptions
            .Include(s => s.SubscriberType)
            .Include(s => s.CommunicationPreferences).ThenInclude(scp => scp.CommunicationPreference)
            .Include(s => s.Interests).ThenInclude(si => si.NewsletterInterest)
            .FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm,
        int? subscriberTypeId,
        int? communicationPreferenceId,
        int? interestId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Subscriptions
            .Include(s => s.SubscriberType)
            .Include(s => s.CommunicationPreferences).ThenInclude(scp => scp.CommunicationPreference)
            .Include(s => s.Interests).ThenInclude(si => si.NewsletterInterest)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                EF.Property<string>(s, "Email").Contains(term));
        }

        if (subscriberTypeId.HasValue)
            query = query.Where(s => s.SubscriberTypeId == subscriberTypeId.Value);

        if (communicationPreferenceId.HasValue)
            query = query.Where(s =>
                s.CommunicationPreferences.Any(scp => scp.CommunicationPreferenceId == communicationPreferenceId.Value));

        if (interestId.HasValue)
            query = query.Where(s =>
                s.Interests.Any(si => si.NewsletterInterestId == interestId.Value));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) =>
        await context.Subscriptions.AddAsync(subscription, cancellationToken);

    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        context.Subscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        context.Subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }
}
