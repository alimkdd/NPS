namespace NewsletterPreferences.Application.DTOs;

public class SubscriptionStatsResponse
{
    public int TotalActive { get; init; }
    public int NewLast7Days { get; init; }
    public int NewLast30Days { get; init; }
}
