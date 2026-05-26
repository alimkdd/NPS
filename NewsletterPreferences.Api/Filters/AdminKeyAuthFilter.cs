using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NewsletterPreferences.Api.Filters;

public class AdminKeyAuthFilter(IConfiguration configuration) : IActionFilter
{
    private const string HeaderName = "X-Admin-Key";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
            providedKey != configuration["AdminSettings:ApiKey"])
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing admin key." });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
