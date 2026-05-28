using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;

namespace NewsletterPreferences.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDataProtectionProvider dataProtectionProvider) : DbContext(options), IUnitOfWork
{
    private readonly IDataProtector _piiProtector =
        dataProtectionProvider.CreateProtector("NewsletterPreferences.PII.v1");

    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriberType> SubscriberTypes => Set<SubscriberType>();
    public DbSet<CommunicationPreference> CommunicationPreferences => Set<CommunicationPreference>();
    public DbSet<NewsletterInterest> NewsletterInterests => Set<NewsletterInterest>();
    public DbSet<SubscriptionCommunicationPreference> SubscriptionCommunicationPreferences => Set<SubscriptionCommunicationPreference>();
    public DbSet<SubscriptionInterest> SubscriptionInterests => Set<SubscriptionInterest>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<WebAuthnCredential> WebAuthnCredentials => Set<WebAuthnCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        var encryptedConverter = new ValueConverter<string?, string?>(
            plain => plain == null ? null : _piiProtector.Protect(plain),
            cipher => cipher == null ? null : _piiProtector.Unprotect(cipher));

        modelBuilder.Entity<Subscription>()
            .Property(s => s.PhoneNumber).HasConversion(encryptedConverter);

        modelBuilder.Entity<Subscription>()
            .Property(s => s.PostalAddress).HasConversion(encryptedConverter);

        modelBuilder.Entity<Subscription>()
            .HasQueryFilter(s => !s.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
