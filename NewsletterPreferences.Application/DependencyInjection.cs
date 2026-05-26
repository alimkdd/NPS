using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NewsletterPreferences.Application.Services;

namespace NewsletterPreferences.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            includeInternalTypes: true);

        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ILookupService, LookupService>();

        return services;
    }
}
