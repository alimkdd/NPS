using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NewsletterPreferences.Application.Common;
using NewsletterPreferences.Application.DTOs.Auth;
using NewsletterPreferences.Application.Settings;
using NewsletterPreferences.Domain.Interfaces;
using System.Text;

namespace NewsletterPreferences.Application.Services;

/// <summary>
/// Orchestrates the four WebAuthn ceremony steps (begin/complete x register/login) and
/// stores the per-ceremony challenge in <see cref="IMemoryCache"/> keyed by an opaque token
/// echoed back to the client. After a successful assertion it issues an admin JWT.
/// </summary>
public class AdminAuthService(
    IFido2 fido2,
    IAdminUserRepository adminUsers,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokens,
    IMemoryCache cache,
    ILogger<AdminAuthService> logger) : IAdminAuthService
{
    private static readonly TimeSpan CeremonyTtl = TimeSpan.FromMinutes(5);
    private const string RegisterCachePrefix = "webauthn:reg:";
    private const string LoginCachePrefix = "webauthn:login:";

    public async Task<Result<AdminAuthStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var admin = await adminUsers.GetByUsernameAsync(GetSeededUsername(), cancellationToken);
        if (admin is null)
            return Result<AdminAuthStatusResponse>.Failure("Admin user has not been provisioned.");

        return Result<AdminAuthStatusResponse>.Success(new AdminAuthStatusResponse
        {
            HasRegisteredCredentials = admin.Credentials.Count > 0,
            Username = admin.Username,
            DisplayName = admin.DisplayName,
        });
    }

    public async Task<Result<RegisterBeginResponse>> BeginRegisterAsync(
        RegisterBeginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return Result<RegisterBeginResponse>.ValidationFailure(["Username is required."]);

        var admin = await adminUsers.GetByUsernameAsync(request.Username, cancellationToken);
        if (admin is null)
            return Result<RegisterBeginResponse>.Failure("Unknown admin user.");

        var user = new Fido2User
        {
            Id = admin.Id.ToByteArray(),
            Name = admin.Username,
            DisplayName = admin.DisplayName,
        };

        var excludeCredentials = admin.Credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var authenticatorSelection = new AuthenticatorSelection
        {
            // Platform = built-in (Windows Hello, Touch ID). Set to null/CrossPlatform to allow security keys too.
            AuthenticatorAttachment = AuthenticatorAttachment.Platform,
            UserVerification = UserVerificationRequirement.Required,
            ResidentKey = ResidentKeyRequirement.Preferred,
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = excludeCredentials,
            AuthenticatorSelection = authenticatorSelection,
            AttestationPreference = AttestationConveyancePreference.None,
        });

        var token = Guid.NewGuid().ToString("N");
        cache.Set(RegisterCachePrefix + token, options, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CeremonyTtl,
            Size = 1,
        });

        logger.LogInformation("WebAuthn registration ceremony initiated for {Username} (token {Token})",
            admin.Username, token);

        return Result<RegisterBeginResponse>.Success(new RegisterBeginResponse
        {
            ChallengeToken = token,
            Options = options,
        });
    }

    public async Task<Result<RegisterCompleteResponse>> CompleteRegisterAsync(
        RegisterCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeToken) || request.AttestationResponse is null)
            return Result<RegisterCompleteResponse>.ValidationFailure(["Challenge token and attestation response are required."]);

        if (!cache.TryGetValue<CredentialCreateOptions>(RegisterCachePrefix + request.ChallengeToken, out var originalOptions)
            || originalOptions is null)
        {
            return Result<RegisterCompleteResponse>.Failure("Registration ceremony has expired. Please start again.");
        }

        var admin = await adminUsers.GetByUsernameAsync(originalOptions.User.Name, cancellationToken);
        if (admin is null)
            return Result<RegisterCompleteResponse>.Failure("Unknown admin user.");

        IsCredentialIdUniqueToUserAsyncDelegate isUnique = async (args, ct) =>
        {
            var existing = await adminUsers.GetByCredentialIdAsync(args.CredentialId, ct);
            return existing is null || existing.Id == admin.Id;
        };

        RegisteredPublicKeyCredential result;
        try
        {
            result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = request.AttestationResponse,
                OriginalOptions = originalOptions,
                IsCredentialIdUniqueToUserCallback = isUnique,
            }, cancellationToken);
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogWarning(ex, "WebAuthn attestation verification failed for {Username}", admin.Username);
            return Result<RegisterCompleteResponse>.Failure("Attestation verification failed.");
        }

        admin.AddCredential(
            credentialId: result.Id,
            publicKey: result.PublicKey,
            signCount: result.SignCount,
            aaGuid: result.AaGuid,
            friendlyName: request.FriendlyName);

        adminUsers.Update(admin);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        cache.Remove(RegisterCachePrefix + request.ChallengeToken);

        logger.LogInformation("WebAuthn credential registered for {Username} (AAGUID {AaGuid})",
            admin.Username, result.AaGuid);

        return Result<RegisterCompleteResponse>.Success(new RegisterCompleteResponse
        {
            Success = true,
            FriendlyName = request.FriendlyName,
        });
    }

    public async Task<Result<LoginBeginResponse>> BeginLoginAsync(
        LoginBeginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return Result<LoginBeginResponse>.ValidationFailure(["Username is required."]);

        var admin = await adminUsers.GetByUsernameAsync(request.Username, cancellationToken);
        if (admin is null || admin.Credentials.Count == 0)
        {
            // Don't disclose whether the username exists.
            return Result<LoginBeginResponse>.Failure("No registered credentials for this user.");
        }

        var allowed = admin.Credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowed,
            UserVerification = UserVerificationRequirement.Required,
        });

        var token = Guid.NewGuid().ToString("N");
        cache.Set(LoginCachePrefix + token, new LoginCacheEntry(options, admin.Username), new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CeremonyTtl,
            Size = 1,
        });

        return Result<LoginBeginResponse>.Success(new LoginBeginResponse
        {
            ChallengeToken = token,
            Options = options,
        });
    }

    public async Task<Result<LoginCompleteResponse>> CompleteLoginAsync(
        LoginCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeToken) || request.AssertionResponse is null)
            return Result<LoginCompleteResponse>.ValidationFailure(["Challenge token and assertion response are required."]);

        if (!cache.TryGetValue<LoginCacheEntry>(LoginCachePrefix + request.ChallengeToken, out var entry)
            || entry is null)
        {
            return Result<LoginCompleteResponse>.Failure("Login ceremony has expired. Please start again.");
        }

        var admin = await adminUsers.GetByUsernameAsync(entry.Username, cancellationToken);
        if (admin is null)
            return Result<LoginCompleteResponse>.Failure("Unknown admin user.");

        var credential = admin.Credentials.FirstOrDefault(c =>
            c.CredentialId.AsSpan().SequenceEqual(request.AssertionResponse.RawId));
        if (credential is null)
            return Result<LoginCompleteResponse>.Failure("Unknown credential.");

        IsUserHandleOwnerOfCredentialIdAsync userHandleOwnerCheck = (args, ct) =>
        {
            // The user handle in the assertion must match this admin's user ID.
            var matches = args.UserHandle.AsSpan().SequenceEqual(admin.Id.ToByteArray());
            return Task.FromResult(matches);
        };

        VerifyAssertionResult verifyResult;
        try
        {
            verifyResult = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = request.AssertionResponse,
                OriginalOptions = entry.Options,
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = credential.SignCount,
                IsUserHandleOwnerOfCredentialIdCallback = userHandleOwnerCheck,
            }, cancellationToken);
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogWarning(ex, "WebAuthn assertion verification failed for {Username}", admin.Username);
            return Result<LoginCompleteResponse>.Failure("Assertion verification failed.");
        }

        credential.RecordSuccessfulAssertion(verifyResult.SignCount);
        adminUsers.Update(admin);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        cache.Remove(LoginCachePrefix + request.ChallengeToken);

        var token = jwtTokens.IssueAdminToken(admin);
        logger.LogInformation("Admin {Username} signed in via WebAuthn", admin.Username);

        return Result<LoginCompleteResponse>.Success(new LoginCompleteResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            DisplayName = admin.DisplayName,
        });
    }

    private string GetSeededUsername()
    {
        // The seeded admin's username is normalized lowercase via AdminUser.Create.
        // The endpoint passes the username explicitly in begin requests; this is only
        // used by the status endpoint to surface whether enrollment is needed.
        return "admin";
    }

    private sealed record LoginCacheEntry(AssertionOptions Options, string Username);
}
