using FluentAssertions;
using NewsletterPreferences.Application.Common;
using NewsletterPreferences.Application.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace NewsletterPreferences.Api.Tests.Controllers;

public class AdminControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AdminControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpRequestMessage AdminRequest(HttpMethod method, string url) =>
        new(method, url) { Headers = { { "X-Admin-Key", TestWebApplicationFactory.AdminKey } } };

    private async Task<Guid> SeedSubscriptionAsync(string email = "admin-test@example.com")
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var payload = new
        {
            firstName = "Test",
            lastName = "User",
            email,
            subscriberTypeId = 1,
            communicationPreferenceIds = new[] { 1 },
            interestIds = new[] { 1 },
            consentGiven = true
        };

        var response = await _client.PostAsJsonAsync("/api/subscriptions", payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<UpsertSubscriptionResult>();
        return result!.SubscriptionId;
    }

    // ── Auth guard ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_WithoutAdminKey_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPaged_WithWrongAdminKey_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/subscriptions");
        request.Headers.Add("X-Admin-Key", "wrong-key");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET paged ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_WithValidKey_Returns200()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.SendAsync(AdminRequest(HttpMethod.Get, "/api/admin/subscriptions"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaged_ReturnsPagedResult()
    {
        await SeedSubscriptionAsync("paged@example.com");

        var response = await _client.SendAsync(AdminRequest(HttpMethod.Get, "/api/admin/subscriptions?page=1&pageSize=10"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<SubscriptionResponse>>();
        body.Should().NotBeNull();
        body!.Items.Should().NotBeNull();
    }

    // ── GET by ID ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WhenExists_Returns200WithSubscription()
    {
        var id = await SeedSubscriptionAsync("getbyid@example.com");

        var response = await _client.SendAsync(AdminRequest(HttpMethod.Get, $"/api/admin/subscriptions/{id}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionResponse>();
        body!.Id.Should().Be(id);
        body.Email.Should().Be("getbyid@example.com");
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.SendAsync(AdminRequest(HttpMethod.Get, $"/api/admin/subscriptions/{Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithoutAdminKey_Returns401()
    {
        var response = await _client.GetAsync($"/api/admin/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DELETE ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var id = await SeedSubscriptionAsync("delete-me@example.com");

        var response = await _client.SendAsync(AdminRequest(HttpMethod.Delete, $"/api/admin/subscriptions/{id}"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WhenNotFound_Returns404()
    {
        await _factory.EnsureDatabaseCreatedAsync();

        var response = await _client.SendAsync(AdminRequest(HttpMethod.Delete, $"/api/admin/subscriptions/{Guid.NewGuid()}"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithoutAdminKey_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/admin/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ThenGetById_Returns404()
    {
        var id = await SeedSubscriptionAsync("delete-then-check@example.com");

        await _client.SendAsync(AdminRequest(HttpMethod.Delete, $"/api/admin/subscriptions/{id}"));
        var getResponse = await _client.SendAsync(AdminRequest(HttpMethod.Get, $"/api/admin/subscriptions/{id}"));

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
