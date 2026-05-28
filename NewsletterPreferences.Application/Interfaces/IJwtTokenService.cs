using NewsletterPreferences.Domain.Entities;

namespace NewsletterPreferences.Application.Interfaces;

public record JwtToken(string AccessToken, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    JwtToken IssueAdminToken(AdminUser admin);
}
