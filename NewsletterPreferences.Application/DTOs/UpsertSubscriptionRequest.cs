namespace NewsletterPreferences.Application.DTOs;

public class UpsertSubscriptionRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Organisation { get; init; }
    public int SubscriberTypeId { get; init; }
    public List<int> CommunicationPreferenceIds { get; init; } = [];
    public List<int> InterestIds { get; init; } = [];
    public string? PhoneNumber { get; init; }
    public string? PostalAddress { get; init; }
    public bool ConsentGiven { get; init; }
}
