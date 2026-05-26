using FluentAssertions;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Validators;

namespace NewsletterPreferences.Application.Tests.Validators;

public class UpsertSubscriptionRequestValidatorTests
{
    private readonly UpsertSubscriptionRequestValidator _sut = new();

    private static UpsertSubscriptionRequest ValidRequest(
        string firstName = "Jane",
        string lastName = "Doe",
        string email = "jane.doe@example.com",
        string? organisation = null,
        int subscriberTypeId = 1,
        List<int>? communicationPreferenceIds = null,
        List<int>? interestIds = null,
        bool consentGiven = true,
        string? phoneNumber = null,
        string? postalAddress = null) => new()
    {
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Organisation = organisation,
        SubscriberTypeId = subscriberTypeId,
        CommunicationPreferenceIds = communicationPreferenceIds ?? [1],
        InterestIds = interestIds ?? [1],
        ConsentGiven = consentGiven,
        PhoneNumber = phoneNumber,
        PostalAddress = postalAddress
    };

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyFirstName_FailsValidation(string firstName)
    {
        var result = await _sut.ValidateAsync(ValidRequest(firstName: firstName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.FirstName));
    }

    [Fact]
    public async Task FirstNameExceedsMaxLength_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(firstName: new string('A', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyLastName_FailsValidation(string lastName)
    {
        var result = await _sut.ValidateAsync(ValidRequest(lastName: lastName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.LastName));
    }

    [Fact]
    public async Task LastNameExceedsMaxLength_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(lastName: new string('B', 101)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyEmail_FailsValidation(string email)
    {
        var result = await _sut.ValidateAsync(ValidRequest(email: email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.Email));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("plainaddress")]
    [InlineData("no-at-sign-here")]
    public async Task InvalidEmailFormat_FailsValidation(string email)
    {
        var result = await _sut.ValidateAsync(ValidRequest(email: email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.Email));
    }

    [Fact]
    public async Task EmailExceedsMaxLength_FailsValidation()
    {
        var longEmail = new string('a', 244) + "@example.com";

        var result = await _sut.ValidateAsync(ValidRequest(email: longEmail));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.Email));
    }

    [Fact]
    public async Task SubscriberTypeIdIsZero_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(subscriberTypeId: 0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.SubscriberTypeId));
    }

    [Fact]
    public async Task EmptyCommunicationPreferences_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(communicationPreferenceIds: []));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.CommunicationPreferenceIds));
    }

    [Fact]
    public async Task EmptyInterestIds_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(interestIds: []));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.InterestIds));
    }

    [Fact]
    public async Task ConsentNotGiven_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(consentGiven: false));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.ConsentGiven));
    }

    [Fact]
    public async Task OrganisationExceedsMaxLength_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(organisation: new string('O', 256)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.Organisation));
    }

    [Fact]
    public async Task NullOrganisation_PassesValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(organisation: null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PhoneNumberExceedsMaxLength_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(phoneNumber: new string('9', 31)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.PhoneNumber));
    }

    [Fact]
    public async Task PostalAddressExceedsMaxLength_FailsValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(postalAddress: new string('P', 501)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertSubscriptionRequest.PostalAddress));
    }

    [Fact]
    public async Task ValidRequestWithAllOptionalFields_PassesValidation()
    {
        var result = await _sut.ValidateAsync(ValidRequest(
            organisation: "Acme Corp",
            phoneNumber: "0412345678",
            postalAddress: "123 Main St, Sydney NSW 2000"));

        result.IsValid.Should().BeTrue();
    }
}
