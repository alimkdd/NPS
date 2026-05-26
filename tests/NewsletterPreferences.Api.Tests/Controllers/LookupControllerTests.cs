using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NewsletterPreferences.Application.DTOs;

namespace NewsletterPreferences.Api.Tests.Controllers;

public class LookupControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public LookupControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithSeedData()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.GetAsync("/api/lookups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LookupsResponse>();
        body.Should().NotBeNull();
        body!.SubscriberTypes.Should().NotBeEmpty();
        body.CommunicationPreferences.Should().NotBeEmpty();
        body.Interests.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAll_IncludesExpectedCommunicationPreferenceCodes()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.GetAsync("/api/lookups");
        var body = await response.Content.ReadFromJsonAsync<LookupsResponse>();

        var codes = body!.CommunicationPreferences.Select(p => p.Code).ToList();
        codes.Should().Contain(["EMAIL", "PHONE", "SMS", "POST"]);
    }

    [Fact]
    public async Task GetAll_IncludesExpectedSubscriberTypeCodes()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.GetAsync("/api/lookups");
        var body = await response.Content.ReadFromJsonAsync<LookupsResponse>();

        body!.SubscriberTypes.Should().Contain(st => st.Code == "HOME_BUYER");
        body.SubscriberTypes.Should().Contain(st => st.Code == "DEVELOPER");
    }
}
