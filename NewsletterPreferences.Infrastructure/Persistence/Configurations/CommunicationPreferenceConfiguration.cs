using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class CommunicationPreferenceConfiguration : IEntityTypeConfiguration<CommunicationPreference>
{
    public void Configure(EntityTypeBuilder<CommunicationPreference> builder)
    {
        builder.ToTable("CommunicationPreferences");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasData(
            CommunicationPreference.Create(1, "Email", "EMAIL"),
            CommunicationPreference.Create(2, "Phone", "PHONE"),
            CommunicationPreference.Create(3, "SMS", "SMS"),
            CommunicationPreference.Create(4, "Post", "POST")
        );
    }
}
