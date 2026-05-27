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
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Subscription?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Subscriptions
            .Include(s => s.SubscriberType)
            .Include(s => s.CommunicationPreferences).ThenInclude(scp => scp.CommunicationPreference)
            .Include(s => s.Interests).ThenInclude(si => si.NewsletterInterest)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant(), cancellationToken);

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
