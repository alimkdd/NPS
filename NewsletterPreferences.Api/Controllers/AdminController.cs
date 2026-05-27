using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NewsletterPreferences.Api.Filters;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Services;

namespace NewsletterPreferences.Api.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
[EnableRateLimiting("admin")]
[ServiceFilter(typeof(AdminKeyAuthFilter))]
[ServiceFilter(typeof(AdminAuditFilter))]
public class AdminController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] SubscriptionFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await subscriptionService.GetPagedAsync(filter, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await subscriptionService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : NotFound(new { error = result.Error });
    }
}
