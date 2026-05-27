using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Interfaces;
using NewsletterPreferences.Application.Services;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;
using NewsletterPreferences.Domain.ValueObjects;

namespace NewsletterPreferences.Application.Tests.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepo = new();
    private readonly Mock<ISubscriptionReadRepository> _subscriptionReadRepo = new();
    private readonly Mock<ILookupRepository> _lookupRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IValidator<UpsertSubscriptionRequest>> _validator = new();
    private readonly SubscriptionService _sut;

    private static readonly IReadOnlyList<CommunicationPreference> AllPreferences =
    [
        CommunicationPreference.Create(1, "Email", "EMAIL"),
        CommunicationPreference.Create(2, "Phone", "PHONE"),
        CommunicationPreference.Create(3, "SMS", "SMS"),
        CommunicationPreference.Create(4, "Post", "POST")
    ];

    public SubscriptionServiceTests()
    {
        _sut = new SubscriptionService(
            _subscriptionRepo.Object,
            _subscriptionReadRepo.Object,
            _lookupRepo.Object,
            _unitOfWork.Object,
            _validator.Object);
    }

    private void SetupValidatorPasses()
    {
        _validator.Setup(v => v.ValidateAsync(It.IsAny<UpsertSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupAllLookupsExist()
    {
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);
        _lookupRepo.Setup(r => r.SubscriberTypeExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lookupRepo.Setup(r => r.AllCommunicationPreferencesExistAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lookupRepo.Setup(r => r.AllInterestsExistAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private static UpsertSubscriptionRequest ValidRequest(
        string email = "jane@example.com",
        List<int>? prefIds = null,
        string? phone = null,
        string? postalAddress = null) => new()
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = email,
            SubscriberTypeId = 1,
            CommunicationPreferenceIds = prefIds ?? [1],
            InterestIds = [1],
            ConsentGiven = true,
            PhoneNumber = phone,
            PostalAddress = postalAddress
        };

    private static Subscription BuildSubscriptionWithNavigations(string email = "jane@example.com")
    {
        var emailObj = Email.Create(email);
        var sub = Subscription.Create("Jane", "Doe", emailObj, null, 1, null, null, true, [], []);

        var subType = SubscriberType.Create(1, "Home Buyer", "HOME_BUYER");
        typeof(Subscription)
            .GetProperty(nameof(Subscription.SubscriberType))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(sub, [subType]);

        return sub;
    }

    // ── UpsertAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertAsync_WhenFluentValidationFails_ReturnsValidationFailure()
    {
        _validator.Setup(v => v.ValidateAsync(It.IsAny<UpsertSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Email", "A valid email address is required.")]));

        var result = await _sut.UpsertAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain("A valid email address is required.");
    }

    [Fact]
    public async Task UpsertAsync_WhenPhoneSelectedAndPhoneMissing_ReturnsConditionalError()
    {
        SetupValidatorPasses();
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);

        // PHONE preference selected, no phone number provided
        var request = ValidRequest(prefIds: [2]);

        var result = await _sut.UpsertAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("Phone number is required"));
    }

    [Fact]
    public async Task UpsertAsync_WhenSmsSelectedAndPhoneMissing_ReturnsConditionalError()
    {
        SetupValidatorPasses();
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);

        // SMS preference selected, no phone number provided
        var request = ValidRequest(prefIds: [3]);

        var result = await _sut.UpsertAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("Phone number is required"));
    }

    [Fact]
    public async Task UpsertAsync_WhenPostSelectedAndAddressMissing_ReturnsConditionalError()
    {
        SetupValidatorPasses();
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);

        // POST preference selected, no postal address provided
        var request = ValidRequest(prefIds: [4]);

        var result = await _sut.UpsertAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("Postal address is required"));
    }

    [Fact]
    public async Task UpsertAsync_WhenPhoneAndSmsSelectedWithPhone_PassesConditionalValidation()
    {
        SetupValidatorPasses();
        SetupAllLookupsExist();
        _subscriptionRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var request = ValidRequest(prefIds: [2, 3], phone: "0412345678");

        var result = await _sut.UpsertAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertAsync_WhenInvalidSubscriberType_ReturnsValidationFailure()
    {
        SetupValidatorPasses();
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);
        _lookupRepo.Setup(r => r.SubscriberTypeExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.UpsertAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("Invalid subscriber type"));
    }

    [Fact]
    public async Task UpsertAsync_WhenInvalidCommunicationPreferences_ReturnsValidationFailure()
    {
        SetupValidatorPasses();
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);
        _lookupRepo.Setup(r => r.SubscriberTypeExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lookupRepo.Setup(r => r.AllCommunicationPreferencesExistAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.UpsertAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("communication preferences are invalid"));
    }

    [Fact]
    public async Task UpsertAsync_WhenInvalidInterestIds_ReturnsValidationFailure()
    {
        SetupValidatorPasses();
        _lookupRepo.Setup(r => r.GetCommunicationPreferencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllPreferences);
        _lookupRepo.Setup(r => r.SubscriberTypeExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lookupRepo.Setup(r => r.AllCommunicationPreferencesExistAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _lookupRepo.Setup(r => r.AllInterestsExistAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.UpsertAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("newsletter interests are invalid"));
    }

    [Fact]
    public async Task UpsertAsync_WhenNewEmail_CreatesSubscription_ReturnsSuccessWithIsUpdateFalse()
    {
        SetupValidatorPasses();
        SetupAllLookupsExist();
        _subscriptionRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await _sut.UpsertAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsUpdate.Should().BeFalse();
        result.Value.SubscriptionId.Should().NotBeEmpty();
        _subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_WhenExistingEmail_UpdatesSubscription_ReturnsIsUpdateTrue()
    {
        SetupValidatorPasses();
        SetupAllLookupsExist();

        var existing = BuildSubscriptionWithNavigations();
        _subscriptionRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _sut.UpsertAsync(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsUpdate.Should().BeTrue();
        result.Value.SubscriptionId.Should().Be(existing.Id);
        _subscriptionRepo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _subscriptionRepo.Verify(r => r.AddAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_WhenSubscriptionNotFound_ReturnsFailure()
    {
        _subscriptionReadRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionResponse?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Subscription not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSubscriptionExists_ReturnsSuccessWithResponse()
    {
        var id = Guid.NewGuid();
        var dto = new SubscriptionResponse
        {
            Id = id,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            SubscriberType = new LookupItemResponse { Id = 1, Name = "Home Buyer", Code = "HOME_BUYER" }
        };
        _subscriptionReadRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
        result.Value.Email.Should().Be("jane@example.com");
        result.Value.FirstName.Should().Be("Jane");
    }

    // ── GetPagedAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResultWithCorrectMetadata()
    {
        IReadOnlyList<SubscriptionResponse> items = [new SubscriptionResponse { Id = Guid.NewGuid() }];
        _subscriptionReadRepo.Setup(r => r.GetPagedAsync(
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var filter = new SubscriptionFilterRequest { Page = 1, PageSize = 20 };
        var result = await _sut.GetPagedAsync(filter, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPagedAsync_ClampsPageSizeAbove100()
    {
        IReadOnlyList<SubscriptionResponse> items = [];
        _subscriptionReadRepo.Setup(r => r.GetPagedAsync(
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 0));

        var filter = new SubscriptionFilterRequest { Page = 1, PageSize = 999 };
        var result = await _sut.GetPagedAsync(filter, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _subscriptionReadRepo.Verify(r => r.GetPagedAsync(
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_WhenSubscriptionNotFound_ReturnsFailure()
    {
        _subscriptionRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Subscription not found.");
        _subscriptionRepo.Verify(r => r.DeleteAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenSubscriptionExists_SoftDeletesAndReturnsSuccess()
    {
        var subscription = BuildSubscriptionWithNavigations();
        _subscriptionRepo.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await _sut.DeleteAsync(subscription.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.IsDeleted.Should().BeTrue();
        subscription.DeletedAt.Should().NotBeNull();
        _subscriptionRepo.Verify(r => r.UpdateAsync(subscription, It.IsAny<CancellationToken>()), Times.Once);
        _subscriptionRepo.Verify(r => r.DeleteAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
