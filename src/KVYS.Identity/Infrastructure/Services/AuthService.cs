using KVYS.Identity.Application.DTOs;
using KVYS.Identity.Application.Services;
using KVYS.Identity.Domain.Entities;
using KVYS.Identity.Infrastructure.Persistence;
using KVYS.Shared.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KVYS.Identity.Infrastructure.Services;

/// <summary>
/// Authentication service implementation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IdentityDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IdentityDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _context = context;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return Result.Failure<LoginResponse>(
                Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Result.Failure<LoginResponse>(
                    Error.Validation("Auth.LockedOut", "Account is locked. Please try again later."));
            }

            return Result.Failure<LoginResponse>(
                Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));
        }

        var (accessToken, expiresAt) = await _jwtService.GenerateAccessTokenAsync(user);
        var refreshToken = _jwtService.GenerateRefreshToken(ipAddress);

        refreshToken.UserId = user.Id;
        _context.RefreshTokens.Add(refreshToken);

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Title,
            roles,
            permissions
        );

        return new LoginResponse(accessToken, refreshToken.Token, expiresAt, userDto);
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result.Failure<LoginResponse>(
                Error.Validation("Auth.InvalidToken", "Invalid or expired refresh token."));
        }

        var user = refreshToken.User;
        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(
                Error.Validation("Auth.UserInactive", "User account is inactive."));
        }

        // Revoke old token
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        // Generate new tokens
        var (accessToken, expiresAt) = await _jwtService.GenerateAccessTokenAsync(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken(ipAddress);
        newRefreshToken.UserId = user.Id;

        refreshToken.ReplacedByToken = newRefreshToken.Token;
        _context.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id);

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Title,
            roles,
            permissions
        );

        return new LoginResponse(accessToken, newRefreshToken.Token, expiresAt, userDto);
    }

    public async Task<Result> LogoutAsync(Guid userId, string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);

        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Result.Success();
    }

    public async Task<Result> RevokeAllTokensAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        return await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.ToString())
            .Distinct()
            .ToListAsync();
    }
}
