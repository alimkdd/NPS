using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("AdminAuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Timestamp).IsRequired();
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.TargetSubscriptionId).HasMaxLength(50);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);
        builder.Property(a => a.ClientIp).HasMaxLength(64);
        builder.Property(a => a.StatusCode).IsRequired();

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.CorrelationId);
    }
}
