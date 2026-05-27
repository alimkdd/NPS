using Microsoft.AspNetCore.Mvc.Filters;
using NewsletterPreferences.Api.Middleware;
using NewsletterPreferences.Domain.Entities;
using NewsletterPreferences.Domain.Interfaces;

namespace NewsletterPreferences.Api.Filters;

public class AdminAuditFilter(
    IAdminAuditLogRepository auditRepository,
    ILogger<AdminAuditFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (executed.Result is Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult)
            return;

        try
        {
            var http = context.HttpContext;
            var action = $"{http.Request.Method} {context.ActionDescriptor.DisplayName}";
            var targetId = context.RouteData.Values.TryGetValue("id", out var id) ? id?.ToString() : null;
            var statusCode = http.Response.StatusCode != 0 ? http.Response.StatusCode : 200;

            var entry = AdminAuditLog.Create(
                action: action,
                targetSubscriptionId: targetId,
                correlationId: http.GetCorrelationId(),
                clientIp: http.Connection.RemoteIpAddress?.ToString(),
                statusCode: statusCode);

            await auditRepository.AddAsync(entry, http.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record admin audit log entry");
        }
    }
}
