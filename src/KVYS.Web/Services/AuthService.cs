using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace KVYS.Web.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login",
                new { Email = email, Password = password });

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed: {Error}", errorContent);
                return new LoginResult(false, "Invalid email or password");
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result == null)
                return new LoginResult(false, "Invalid response from server");

            await _localStorage.SetItemAsync("accessToken", result.AccessToken);
            await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

            ((KvysAuthStateProvider)_authStateProvider).NotifyAuthenticationStateChanged();

            return new LoginResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return new LoginResult(false, "An error occurred during login");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _httpClient.PostAsJsonAsync("api/v1/auth/logout",
                    new { RefreshToken = refreshToken });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout API call");
        }

        await _localStorage.RemoveItemAsync("accessToken");
        await _localStorage.RemoveItemAsync("refreshToken");

        ((KvysAuthStateProvider)_authStateProvider).NotifyAuthenticationStateChanged();
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("accessToken");
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
                return null;

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                ?? email;
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value
                ?? "User";
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            return new UserInfo(
                userId ?? "",
                name ?? "",
                email ?? "",
                role
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JWT token");
            return null;
        }
    }
}

public record LoginResult(bool Success, string? ErrorMessage);
public record UserInfo(string Id, string Name, string Email, string Role);

public class KvysAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<KvysAuthStateProvider> _logger;
    private static readonly AuthenticationState AnonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public KvysAuthStateProvider(
        ILocalStorageService localStorage,
        ILogger<KvysAuthStateProvider> logger)
    {
        _localStorage = localStorage;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");
            if (string.IsNullOrEmpty(token))
                return AnonymousState;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync("accessToken");
                return AnonymousState;
            }

            var claims = jwtToken.Claims.ToList();

            // Add standard claim types if not present
            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var nameClaim = claims.FirstOrDefault(c => c.Type == "name" || c.Type == "email");
                if (nameClaim != null)
                    claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return AnonymousState;
        }
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
