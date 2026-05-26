namespace NewsletterPreferences.Application.DTOs;

public class SubscriptionResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Organisation { get; init; }
    public LookupItemResponse SubscriberType { get; init; } = null!;
    public IReadOnlyList<LookupItemResponse> CommunicationPreferences { get; init; } = [];
    public IReadOnlyList<LookupItemResponse> Interests { get; init; } = [];
    public string? PhoneNumber { get; init; }
    public string? PostalAddress { get; init; }
    public bool ConsentGiven { get; init; }
    public DateTime ConsentTimestamp { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
