namespace NewsletterPreferences.Domain.Entities;

public class AdminAuditLog
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? TargetSubscriptionId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? ClientIp { get; private set; }
    public int StatusCode { get; private set; }

    private AdminAuditLog() { }

    public static AdminAuditLog Create(
        string action,
        string? targetSubscriptionId,
        string? correlationId,
        string? clientIp,
        int statusCode) => new()
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Action = action,
            TargetSubscriptionId = targetSubscriptionId,
            CorrelationId = correlationId,
            ClientIp = clientIp,
            StatusCode = statusCode
        };
}
