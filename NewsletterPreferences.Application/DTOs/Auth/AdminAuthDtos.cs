using Fido2NetLib;

namespace NewsletterPreferences.Application.DTOs.Auth;

public class RegisterBeginRequest
{
    public string Username { get; init; } = string.Empty;
}

public class RegisterBeginResponse
{
    /// <summary>
    /// Opaque server-issued token tying the begin/complete ceremony pair together.
    /// Echoed back by the client in <see cref="RegisterCompleteRequest"/>.
    /// </summary>
    public string ChallengeToken { get; init; } = string.Empty;
    public CredentialCreateOptions Options { get; init; } = null!;
}

public class RegisterCompleteRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
    public AuthenticatorAttestationRawResponse AttestationResponse { get; init; } = null!;
    public string? FriendlyName { get; init; }
}

public class RegisterCompleteResponse
{
    public bool Success { get; init; }
    public string? FriendlyName { get; init; }
}

public class LoginBeginRequest
{
    public string Username { get; init; } = string.Empty;
}

public class LoginBeginResponse
{
    public string ChallengeToken { get; init; } = string.Empty;
    public AssertionOptions Options { get; init; } = null!;
}

public class LoginCompleteRequest
{
    public string ChallengeToken { get; init; } = string.Empty;
    public AuthenticatorAssertionRawResponse AssertionResponse { get; init; } = null!;
}

public class LoginCompleteResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public class AdminAuthStatusResponse
{
    /// <summary>
    /// True once the bootstrapped admin has registered at least one passkey.
    /// The client uses this to decide whether to show the first-time enrollment flow.
    /// </summary>
    public bool HasRegisteredCredentials { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
