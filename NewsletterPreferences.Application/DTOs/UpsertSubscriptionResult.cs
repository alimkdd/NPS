namespace NewsletterPreferences.Application.DTOs;

public class UpsertSubscriptionResult
{
    public Guid SubscriptionId { get; init; }
    public bool IsUpdate { get; init; }
}
