using Microsoft.AspNetCore.WebUtilities;

namespace NewsletterPreferences.Api.Middleware;

public static class QueryStringRedactor
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "searchTerm", "email", "phone", "phoneNumber", "postalAddress",
        "token", "apiKey", "key", "password", "secret"
    };

    public static string? Redact(string? rawQueryString)
    {
        if (string.IsNullOrEmpty(rawQueryString)) return rawQueryString;

        var parsed = QueryHelpers.ParseQuery(rawQueryString);
        if (parsed.Count == 0) return rawQueryString;

        var rebuilt = parsed.ToDictionary(
            kv => kv.Key,
            kv => SensitiveKeys.Contains(kv.Key) ? "****" : kv.Value.ToString());

        return QueryString.Create(rebuilt!).Value;
    }
}