namespace NewsletterPreferences.Domain.Entities;

public class SubscriptionInterest
{
    public Guid SubscriptionId { get; private set; }
    public int NewsletterInterestId { get; private set; }

    public NewsletterInterest NewsletterInterest { get; private set; } = null!;

    private SubscriptionInterest() { }

    public SubscriptionInterest(Guid subscriptionId, int newsletterInterestId)
    {
        SubscriptionId = subscriptionId;
        NewsletterInterestId = newsletterInterestId;
    }
}
