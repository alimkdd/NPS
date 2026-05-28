using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsletterPreferences.Application.Settings;
using System.Text;

namespace NewsletterPreferences.Api.Authentication;

/// <summary>
/// Configures <see cref="JwtBearerOptions"/> lazily from <see cref="JwtSettings"/>.
/// Doing this via an <see cref="IConfigureNamedOptions{TOptions}"/> rather than inline
/// during <c>AddJwtBearer(...)</c> ensures the bearer middleware sees the same fully
/// merged configuration as the rest of the app — important for integration tests that
/// override JWT settings via <c>ConfigureAppConfiguration</c>.
/// </summary>
public class ConfigureJwtBearerOptions(IOptions<JwtSettings> jwtSettings, IHostEnvironment env)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) =>
        Configure(JwtBearerDefaults.AuthenticationScheme, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var settings = jwtSettings.Value;
        var signingKey = string.IsNullOrWhiteSpace(settings.SigningKey)
            ? new string('x', 32) // boot-time placeholder so the middleware can register; never accepts real tokens
            : settings.SigningKey;

        options.RequireHttpsMetadata = !env.IsDevelopment() && !env.IsEnvironment("Test");
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    }
}
