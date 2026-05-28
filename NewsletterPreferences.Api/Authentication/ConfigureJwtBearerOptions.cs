using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsletterPreferences.Application.Settings;
using System.Text;

namespace NewsletterPreferences.Api.Authentication;

public class ConfigureJwtBearerOptions(IOptions<JwtSettings> jwtSettings, IHostEnvironment env)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var settings = jwtSettings.Value;
        var signingKey = string.IsNullOrWhiteSpace(settings.SigningKey)
            ? new string('x', 32)
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