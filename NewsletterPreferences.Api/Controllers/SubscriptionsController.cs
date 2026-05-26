using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Services;

namespace NewsletterPreferences.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("public")]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await subscriptionService.UpsertAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new
            {
                errors = result.ValidationErrors.Count > 0
                    ? result.ValidationErrors
                    : (IEnumerable<string?>)[result.Error]
            });

        return result.Value!.IsUpdate
            ? Ok(result.Value)
            : CreatedAtAction(nameof(Upsert), result.Value);
    }
}
