namespace KVYS.Identity.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record RefreshTokenRequest(string RefreshToken);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Title,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions
);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Title
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
