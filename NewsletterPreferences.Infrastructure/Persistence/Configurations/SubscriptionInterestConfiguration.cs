using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class SubscriptionInterestConfiguration : IEntityTypeConfiguration<SubscriptionInterest>
{
    public void Configure(EntityTypeBuilder<SubscriptionInterest> builder)
    {
        builder.ToTable("SubscriptionInterests");
        builder.HasKey(si => new { si.SubscriptionId, si.NewsletterInterestId });

        builder.HasOne(si => si.NewsletterInterest)
            .WithMany()
            .HasForeignKey(si => si.NewsletterInterestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
