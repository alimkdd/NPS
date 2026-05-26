namespace NewsletterPreferences.Domain.Entities;

public class SubscriptionCommunicationPreference
{
    public Guid SubscriptionId { get; private set; }
    public int CommunicationPreferenceId { get; private set; }

    public CommunicationPreference CommunicationPreference { get; private set; } = null!;

    private SubscriptionCommunicationPreference() { }

    public SubscriptionCommunicationPreference(Guid subscriptionId, int communicationPreferenceId)
    {
        SubscriptionId = subscriptionId;
        CommunicationPreferenceId = communicationPreferenceId;
    }
}
