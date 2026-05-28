using NewsletterPreferences.Application.Common;
using NewsletterPreferences.Application.DTOs.Auth;

namespace NewsletterPreferences.Application.Services;

public interface IAdminAuthService
{
    Task<Result<AdminAuthStatusResponse>> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<RegisterBeginResponse>> BeginRegisterAsync(RegisterBeginRequest request, CancellationToken cancellationToken = default);
    Task<Result<RegisterCompleteResponse>> CompleteRegisterAsync(RegisterCompleteRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginBeginResponse>> BeginLoginAsync(LoginBeginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginCompleteResponse>> CompleteLoginAsync(LoginCompleteRequest request, CancellationToken cancellationToken = default);
}
