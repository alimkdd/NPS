using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Application.Services;

public interface ILookupService
{
    Task<LookupsResponse> GetAllLookupsAsync(CancellationToken cancellationToken = default);
}
