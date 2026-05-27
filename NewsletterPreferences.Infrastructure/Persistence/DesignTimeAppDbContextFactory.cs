using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NewsletterPreferences.Infrastructure.Persistence;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=NewsletterPreferencesDb_Design;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new AppDbContext(options, new EphemeralDataProtectionProvider());
    }
}
