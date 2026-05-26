using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class SubscriptionCommunicationPreferenceConfiguration
    : IEntityTypeConfiguration<SubscriptionCommunicationPreference>
{
    public void Configure(EntityTypeBuilder<SubscriptionCommunicationPreference> builder)
    {
        builder.ToTable("SubscriptionCommunicationPreferences");
        builder.HasKey(scp => new { scp.SubscriptionId, scp.CommunicationPreferenceId });

        builder.HasOne(scp => scp.CommunicationPreference)
            .WithMany()
            .HasForeignKey(scp => scp.CommunicationPreferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
