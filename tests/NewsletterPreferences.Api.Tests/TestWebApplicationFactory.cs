using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure.Persistence;

namespace NewsletterPreferences.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminKey = "test-admin-key-123";

    // One fixed DB name per factory instance so all scopes share the same store
    private readonly string _dbName = "NpsTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSettings:ApiKey"] = AdminKey,
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core service descriptors that reference AppDbContext.
            // EF Core 10 registers IDbContextOptionsConfiguration<TContext> as a Singleton
            // in addition to DbContextOptions<TContext>, so we must remove everything
            // that has AppDbContext as a generic type argument or service type.
            var toRemove = services
                .Where(d => IsAppDbContextRelated(d))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        });
    }

    private static bool IsAppDbContextRelated(ServiceDescriptor d)
    {
        if (d.ServiceType == typeof(AppDbContext)) return true;
        if (d.ServiceType == typeof(DbContextOptions)) return true;

        if (!d.ServiceType.IsGenericType) return false;

        var typeArgs = d.ServiceType.GenericTypeArguments;
        return typeArgs.Length == 1 && typeArgs[0] == typeof(AppDbContext);
    }

    /// <summary>
    /// Ensures the in-memory database exists and lookup seed data is present.
    /// EF Core InMemory does not reliably apply HasData seeds via EnsureCreated
    /// across service scopes, so lookup data is seeded explicitly here.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (db.SubscriberTypes.Any()) return;

        db.SubscriberTypes.AddRange(
            SubscriberType.Create(1, "Home Buyer", "HOME_BUYER"),
            SubscriberType.Create(2, "Home Builder", "HOME_BUILDER"),
            SubscriberType.Create(3, "Land Agent / Land Sourcer", "LAND_AGENT"),
            SubscriberType.Create(4, "Developer", "DEVELOPER"),
            SubscriberType.Create(5, "Other", "OTHER")
        );
        db.CommunicationPreferences.AddRange(
            CommunicationPreference.Create(1, "Email", "EMAIL"),
            CommunicationPreference.Create(2, "Phone", "PHONE"),
            CommunicationPreference.Create(3, "SMS", "SMS"),
            CommunicationPreference.Create(4, "Post", "POST")
        );
        db.NewsletterInterests.AddRange(
            NewsletterInterest.Create(1, "Houses", "HOUSES"),
            NewsletterInterest.Create(2, "Apartments", "APARTMENTS"),
            NewsletterInterest.Create(3, "Shared Ownership", "SHARED_OWNERSHIP"),
            NewsletterInterest.Create(4, "Rental", "RENTAL"),
            NewsletterInterest.Create(5, "Land Sourcing", "LAND_SOURCING"),
            NewsletterInterest.Create(6, "Development Finance", "DEV_FINANCE"),
            NewsletterInterest.Create(7, "Planning Updates", "PLANNING_UPDATES"),
            NewsletterInterest.Create(8, "New Developments", "NEW_DEVELOPMENTS")
        );
        await db.SaveChangesAsync();
    }
}
