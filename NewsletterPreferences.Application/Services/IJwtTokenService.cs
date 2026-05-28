using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Application.Services;

public record JwtToken(string AccessToken, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    JwtToken IssueAdminToken(AdminUser admin);
}
