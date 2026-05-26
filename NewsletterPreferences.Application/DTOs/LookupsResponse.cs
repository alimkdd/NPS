namespace NewsletterPreferences.Application.DTOs;

public class LookupsResponse
{
    public IReadOnlyList<LookupItemResponse> SubscriberTypes { get; init; } = [];
    public IReadOnlyList<LookupItemResponse> CommunicationPreferences { get; init; } = [];
    public IReadOnlyList<LookupItemResponse> Interests { get; init; } = [];
}
