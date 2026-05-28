using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Application.Interfaces;

public interface ILookupService
{
    Task<LookupsResponse> GetAllLookupsAsync(CancellationToken cancellationToken = default);
}
