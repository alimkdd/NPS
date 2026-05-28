using Fido2NetLib;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NewsletterPreferences.Api.Authentication;
using NewsletterPreferences.Api.Filters;
using NewsletterPreferences.Api.Middleware;
using NewsletterPreferences.Application;
using NewsletterPreferences.Application.Settings;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Infrastructure;
using NewsletterPreferences.Infrastructure.Persistence;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "NewsletterPreferences.Api");
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 16 * 1024;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.MaxDepth = 8;
});

var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("NewsletterPreferences");

if (!builder.Environment.IsEnvironment("Test"))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeyPath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "keys")));
}

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AdminAuthSettings>(builder.Configuration.GetSection(AdminAuthSettings.SectionName));

// WebAuthn / FIDO2: RP ID is the bare domain (without scheme/port); Origins MUST
// include the scheme+port the FE is served from. WebAuthn allows http only for
// localhost — every other origin must be https.
builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = builder.Configuration["Fido2:ServerName"] ?? "Newsletter Preferences";
    options.Origins = (builder.Configuration.GetSection("Fido2:Origins").Get<string[]>()
                      ?? ["https://localhost:5173"]).ToHashSet();
    options.TimestampDriftTolerance = 300_000; // 5 min
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<AdminAuditFilter>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Newsletter Preferences API", Version = "v1" });
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization", CorrelationIdMiddleware.HeaderName)
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("public", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("admin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    static string ClientKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
});

var app = builder.Build();

app.UseCorrelationId();
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Test"))
    app.UseHsts();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.GetCorrelationId());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("QueryString", QueryStringRedactor.Redact(httpContext.Request.QueryString.Value));
    };
});

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsEnvironment("Test"))
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    // Seed the bootstrapped admin user if missing. Credentials are added through the
    // WebAuthn enrollment ceremony — no password to seed.
    var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminUserRepository>();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var adminSettings = builder.Configuration.GetSection(AdminAuthSettings.SectionName).Get<AdminAuthSettings>()
        ?? new AdminAuthSettings();
    var existing = await adminRepo.GetByUsernameAsync(adminSettings.Username);
    if (existing is null)
    {
        var admin = AdminUser.Create(adminSettings.Username, adminSettings.DisplayName);
        await adminRepo.AddAsync(admin);
        await unitOfWork.SaveChangesAsync();
        Log.Information("Seeded initial admin user {Username}", admin.Username);
    }
}

try
{
    Log.Information("Starting Newsletter Preferences API in {Environment} mode", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
