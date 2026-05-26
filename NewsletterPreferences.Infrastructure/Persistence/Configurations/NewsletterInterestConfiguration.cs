using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class NewsletterInterestConfiguration : IEntityTypeConfiguration<NewsletterInterest>
{
    public void Configure(EntityTypeBuilder<NewsletterInterest> builder)
    {
        builder.ToTable("NewsletterInterests");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Name).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(n => n.Code).IsUnique();

        builder.HasData(
            NewsletterInterest.Create(1, "Houses", "HOUSES"),
            NewsletterInterest.Create(2, "Apartments", "APARTMENTS"),
            NewsletterInterest.Create(3, "Shared Ownership", "SHARED_OWNERSHIP"),
            NewsletterInterest.Create(4, "Rental", "RENTAL"),
            NewsletterInterest.Create(5, "Land Sourcing", "LAND_SOURCING"),
            NewsletterInterest.Create(6, "Development Finance", "DEV_FINANCE"),
            NewsletterInterest.Create(7, "Planning Updates", "PLANNING_UPDATES"),
            NewsletterInterest.Create(8, "New Developments", "NEW_DEVELOPMENTS")
        );
    }
}
