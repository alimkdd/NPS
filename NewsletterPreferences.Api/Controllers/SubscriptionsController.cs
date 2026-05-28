using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NewsletterPreferences.Api.Middleware;
using NewsletterPreferences.Application.DTOs;
using NewsletterPreferences.Application.Interfaces;

namespace NewsletterPreferences.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[EnableRateLimiting("public")]
public class SubscriptionsController(
    ISubscriptionService subscriptionService,
    ILogger<SubscriptionsController> logger) : ControllerBase
{
    [HttpPost]
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

        var correlationId = HttpContext.GetCorrelationId();
        logger.LogInformation(
            "Subscription upsert succeeded. SubscriptionId={SubscriptionId} IsUpdate={IsUpdate} CorrelationId={CorrelationId}",
            result.Value!.SubscriptionId, result.Value.IsUpdate, correlationId);

        return Accepted(new
        {
            subscriptionId = result.Value.SubscriptionId,
            correlationId,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] UnsubscribeRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.GetCorrelationId();
        var result = await subscriptionService.UnsubscribeAsync(request, cancellationToken);
        logger.LogInformation(
            "Unsubscribe request processed. Found={Found} CorrelationId={CorrelationId}",
            result.IsSuccess, correlationId);

        return Accepted(new
        {
            message = "If a matching subscription exists, it has been removed.",
            correlationId,
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}