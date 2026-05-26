using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Api.Tests.Controllers;

public class SubscriptionsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public SubscriptionsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static object ValidPayload(string email = "subscriber@example.com") => new
    {
        firstName = "John",
        lastName = "Smith",
        email,
        subscriberTypeId = 1,
        communicationPreferenceIds = new[] { 1 },
        interestIds = new[] { 1 },
        consentGiven = true
    };

    [Fact]
    public async Task Post_WithValidRequest_Returns201Created()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.PostAsJsonAsync("/api/subscriptions", ValidPayload("new1@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UpsertSubscriptionResult>();
        body!.SubscriptionId.Should().NotBeEmpty();
        body.IsUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task Post_WithDuplicateEmail_Returns200OkWithIsUpdateTrue()
    {
        await _factory.EnsureDatabaseCreatedAsync();
        const string email = "dup@example.com";

        // First create
        await _client.PostAsJsonAsync("/api/subscriptions", ValidPayload(email));

        // Second upsert with same email
        var response = await _client.PostAsJsonAsync("/api/subscriptions", ValidPayload(email));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UpsertSubscriptionResult>();
        body!.IsUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task Post_WithMissingFirstName_Returns400BadRequest()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "",
            lastName = "Smith",
            email = "bad@example.com",
            subscriberTypeId = 1,
            communicationPreferenceIds = new[] { 1 },
            interestIds = new[] { 1 },
            consentGiven = true
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithInvalidEmail_Returns400BadRequest()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "not-an-email",
            subscriberTypeId = 1,
            communicationPreferenceIds = new[] { 1 },
            interestIds = new[] { 1 },
            consentGiven = true
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithConsentFalse_Returns400BadRequest()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "noconsent@example.com",
            subscriberTypeId = 1,
            communicationPreferenceIds = new[] { 1 },
            interestIds = new[] { 1 },
            consentGiven = false
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenPhonePreferenceSelectedWithoutPhone_Returns400BadRequest()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "phonemissing@example.com",
            subscriberTypeId = 1,
            communicationPreferenceIds = new[] { 2 }, // PHONE
            interestIds = new[] { 1 },
            consentGiven = true,
            phoneNumber = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WhenPostPreferenceSelectedWithoutAddress_Returns400BadRequest()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "postmissing@example.com",
            subscriberTypeId = 1,
            communicationPreferenceIds = new[] { 4 }, // POST
            interestIds = new[] { 1 },
            consentGiven = true,
            postalAddress = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithEmptyCommunicationPreferences_Returns400BadRequest()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "nopref@example.com",
            subscriberTypeId = 1,
            communicationPreferenceIds = Array.Empty<int>(),
            interestIds = new[] { 1 },
            consentGiven = true
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
