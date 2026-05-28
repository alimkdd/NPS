using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Infrastructure.Persistence.Configurations;

public class WebAuthnCredentialConfiguration : IEntityTypeConfiguration<WebAuthnCredential>
{
    public void Configure(EntityTypeBuilder<WebAuthnCredential> builder)
    {
        builder.ToTable("WebAuthnCredentials");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AdminUserId).IsRequired();

        builder.Property(c => c.CredentialId)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(c => c.PublicKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(c => c.SignCount).IsRequired();
        builder.Property(c => c.AaGuid).IsRequired();
        builder.Property(c => c.FriendlyName).HasMaxLength(128);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.LastUsedAt);

        // CredentialId is the lookup key during the assertion ceremony — index it.
        builder.HasIndex(c => c.CredentialId).IsUnique();
        builder.HasIndex(c => c.AdminUserId);
    }
}
