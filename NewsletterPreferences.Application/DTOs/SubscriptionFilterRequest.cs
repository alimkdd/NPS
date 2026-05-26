namespace NewsletterPreferences.Application.DTOs;

public class SubscriptionFilterRequest
{
    public string? SearchTerm { get; init; }
    public int? SubscriberTypeId { get; init; }
    public int? CommunicationPreferenceId { get; init; }
    public int? InterestId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
