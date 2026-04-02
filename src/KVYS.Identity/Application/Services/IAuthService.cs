using KVYS.Identity.Application.DTOs;
using KVYS.Shared.Application.Abstractions;

namespace KVYS.Identity.Application.Services;

/// <summary>
/// Authentication service interface.
/// </summary>
public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null);
    Task<Result> LogoutAsync(Guid userId, string refreshToken);
    Task<Result> RevokeAllTokensAsync(Guid userId);
}
