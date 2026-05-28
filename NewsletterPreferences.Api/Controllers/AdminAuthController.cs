using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NewsletterPreferences.Application.DTOs.Auth;
using NewsletterPreferences.Application.Interfaces;

namespace NewsletterPreferences.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
[EnableRateLimiting("admin")]
public class AdminAuthController(IAdminAuthService authService) : ControllerBase
{
    /// <summary>
    /// Returns whether the admin has any registered passkey. The frontend uses this
    /// to decide between the sign-in path and the first-time enrollment path.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var result = await authService.GetStatusAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Begin WebAuthn registration ceremony. Allowed without auth only when the admin
    /// has zero credentials registered yet (bootstrap) — otherwise requires an existing JWT.
    /// </summary>
    [HttpPost("register/begin")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterBegin(
        [FromBody] RegisterBeginRequest request,
        CancellationToken cancellationToken)
    {
        // Bootstrap gate: only allow anonymous registration if no credentials exist yet.
        var status = await authService.GetStatusAsync(cancellationToken);
        if (status.IsSuccess && status.Value!.HasRegisteredCredentials && !User.Identity!.IsAuthenticated)
            return Unauthorized(new { error = "Initial admin enrollment has already been completed." });

        var result = await authService.BeginRegisterAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, validationErrors = result.ValidationErrors });
    }

    [HttpPost("register/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterComplete(
        [FromBody] RegisterCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var status = await authService.GetStatusAsync(cancellationToken);
        if (status.IsSuccess && status.Value!.HasRegisteredCredentials && !User.Identity!.IsAuthenticated)
            return Unauthorized(new { error = "Initial admin enrollment has already been completed." });

        var result = await authService.CompleteRegisterAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, validationErrors = result.ValidationErrors });
    }

    [HttpPost("login/begin")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginBegin(
        [FromBody] LoginBeginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.BeginLoginAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, validationErrors = result.ValidationErrors });
    }

    [HttpPost("login/complete")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginComplete(
        [FromBody] LoginCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.CompleteLoginAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(new { error = result.Error, validationErrors = result.ValidationErrors });
    }
}