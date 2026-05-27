using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.ValueObjects;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);

        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value));

        builder.HasIndex(s => s.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_Subscriptions_CreatedAt_Live")
            .HasFilter("[IsDeleted] = 0");

        builder.Property(s => s.Organisation).HasMaxLength(255);
        builder.Property(s => s.PhoneNumber).HasMaxLength(500);
        builder.Property(s => s.PostalAddress).HasMaxLength(2000);
        builder.Property(s => s.ConsentGiven).IsRequired();
        builder.Property(s => s.ConsentTimestamp).IsRequired();
        builder.Property(s => s.IsDeleted).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasOne(s => s.SubscriberType)
            .WithMany()
            .HasForeignKey(s => s.SubscriberTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.CommunicationPreferences)
            .WithOne()
            .HasForeignKey(scp => scp.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Interests)
            .WithOne()
            .HasForeignKey(si => si.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
