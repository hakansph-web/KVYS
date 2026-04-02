using KVYS.Identity.Domain.Entities;

namespace KVYS.Identity.Application.Services;

/// <summary>
/// JWT token generation and validation service.
/// </summary>
public interface IJwtService
{
    Task<(string AccessToken, DateTime ExpiresAt)> GenerateAccessTokenAsync(ApplicationUser user);
    RefreshToken GenerateRefreshToken(string? ipAddress = null);
    bool ValidateToken(string token);
}
