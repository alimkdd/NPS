using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class SubscriberTypeConfiguration : IEntityTypeConfiguration<SubscriberType>
{
    public void Configure(EntityTypeBuilder<SubscriberType> builder)
    {
        builder.ToTable("SubscriberTypes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();

        builder.HasData(
            SubscriberType.Create(1, "Home Buyer", "HOME_BUYER"),
            SubscriberType.Create(2, "Home Builder", "HOME_BUILDER"),
            SubscriberType.Create(3, "Land Agent / Land Sourcer", "LAND_AGENT"),
            SubscriberType.Create(4, "Developer", "DEVELOPER"),
            SubscriberType.Create(5, "Other", "OTHER")
        );
    }
}
