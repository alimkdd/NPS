using NewsletterPreferences.Domain.Common;
using NewsletterPreferences.Domain.ValueObjects;

namespace NewsletterPreferences.Domain.Entities;

public class Subscription : AuditableEntity
{
    private readonly List<SubscriptionCommunicationPreference> _communicationPreferences = [];
    private readonly List<SubscriptionInterest> _interests = [];

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    // Stored as plain string for queryability (LIKE in admin search). The Email
    // value object enforces format invariants at Create() and at the service layer.
    public string Email { get; private set; } = string.Empty;
    public string? Organisation { get; private set; }
    public int SubscriberTypeId { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? PostalAddress { get; private set; }
    public bool ConsentGiven { get; private set; }
    public DateTime ConsentTimestamp { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public SubscriberType SubscriberType { get; private set; } = null!;
    public IReadOnlyCollection<SubscriptionCommunicationPreference> CommunicationPreferences
        => _communicationPreferences.AsReadOnly();
    public IReadOnlyCollection<SubscriptionInterest> Interests
        => _interests.AsReadOnly();

    private Subscription() { }

    public static Subscription Create(
        string firstName,
        string lastName,
        Email email,
        string? organisation,
        int subscriberTypeId,
        string? phoneNumber,
        string? postalAddress,
        bool consentGiven,
        IEnumerable<int> communicationPreferenceIds,
        IEnumerable<int> interestIds)
    {
        if (!consentGiven)
            throw new InvalidOperationException("Consent must be given to create a subscription.");

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Value,
            Organisation = string.IsNullOrWhiteSpace(organisation) ? null : organisation.Trim(),
            SubscriberTypeId = subscriberTypeId,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            PostalAddress = string.IsNullOrWhiteSpace(postalAddress) ? null : postalAddress.Trim(),
            ConsentGiven = true,
            ConsentTimestamp = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var prefId in communicationPreferenceIds)
            subscription._communicationPreferences.Add(new SubscriptionCommunicationPreference(subscription.Id, prefId));

        foreach (var interestId in interestIds)
            subscription._interests.Add(new SubscriptionInterest(subscription.Id, interestId));

        return subscription;
    }

    public void UpdatePreferences(
        string firstName,
        string lastName,
        string? organisation,
        int subscriberTypeId,
        string? phoneNumber,
        string? postalAddress,
        IEnumerable<int> communicationPreferenceIds,
        IEnumerable<int> interestIds)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Organisation = string.IsNullOrWhiteSpace(organisation) ? null : organisation.Trim();
        SubscriberTypeId = subscriberTypeId;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        PostalAddress = string.IsNullOrWhiteSpace(postalAddress) ? null : postalAddress.Trim();
        ConsentTimestamp = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        _communicationPreferences.Clear();
        foreach (var prefId in communicationPreferenceIds)
            _communicationPreferences.Add(new SubscriptionCommunicationPreference(Id, prefId));

        _interests.Clear();
        foreach (var interestId in interestIds)
            _interests.Add(new SubscriptionInterest(Id, interestId));
    }

    public void MarkAsDeleted()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
