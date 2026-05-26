using Microsoft.EntityFrameworkCore;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;

namespace NewsletterPreferences.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriberType> SubscriberTypes => Set<SubscriberType>();
    public DbSet<CommunicationPreference> CommunicationPreferences => Set<CommunicationPreference>();
    public DbSet<NewsletterInterest> NewsletterInterests => Set<NewsletterInterest>();
    public DbSet<SubscriptionCommunicationPreference> SubscriptionCommunicationPreferences => Set<SubscriptionCommunicationPreference>();
    public DbSet<SubscriptionInterest> SubscriptionInterests => Set<SubscriptionInterest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
