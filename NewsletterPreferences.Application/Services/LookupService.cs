using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Domain.Interfaces;

namespace NewsletterPreferences.Application.Services;

public class LookupService(ILookupRepository lookupRepository) : ILookupService
{
    public async Task<LookupsResponse> GetAllLookupsAsync(CancellationToken cancellationToken = default)
    {
        var subscriberTypes = await lookupRepository.GetSubscriberTypesAsync(cancellationToken);
        var communicationPreferences = await lookupRepository.GetCommunicationPreferencesAsync(cancellationToken);
        var interests = await lookupRepository.GetNewsletterInterestsAsync(cancellationToken);

        return new LookupsResponse
        {
            SubscriberTypes = subscriberTypes
                .Select(s => new LookupItemResponse { Id = s.Id, Name = s.Name, Code = s.Code })
                .ToList(),
            CommunicationPreferences = communicationPreferences
                .Select(c => new LookupItemResponse { Id = c.Id, Name = c.Name, Code = c.Code })
                .ToList(),
            Interests = interests
                .Select(i => new LookupItemResponse { Id = i.Id, Name = i.Name, Code = i.Code })
                .ToList()
        };
    }
}
