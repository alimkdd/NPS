using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NewsletterPreferences.Api.Filters;

public class AdminKeyAuthFilter(IConfiguration configuration) : IActionFilter
{
    private const string HeaderName = "X-Admin-Key";
    private const string ConfigKey = "AdminSettings:ApiKeyHash";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var expectedHashHex = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(expectedHashHex))
        {
            context.Result = new UnauthorizedObjectResult(
                new { error = "Admin authentication is not configured." });
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var provided)
            || string.IsNullOrWhiteSpace(provided.ToString()))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing admin key." });
            return;
        }

        byte[] expectedHash;
        try { expectedHash = Convert.FromHexString(expectedHashHex); }
        catch (FormatException)
        {
            context.Result = new UnauthorizedObjectResult(
                new { error = "Admin authentication is not configured correctly." });
            return;
        }

        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString()));

        if (expectedHash.Length != providedHash.Length
            || !CryptographicOperations.FixedTimeEquals(expectedHash, providedHash))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing admin key." });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
