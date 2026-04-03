using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;

namespace KVYS.Web.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<ApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("accessToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.GetAsync(endpoint);
                }
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.PostAsJsonAsync(endpoint, data);
                }
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.PostAsJsonAsync(endpoint, data);
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting to {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.PutAsJsonAsync(endpoint, data);
                }
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error putting to {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.PutAsJsonAsync(endpoint, data);
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error putting to {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<bool> PatchAsync(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint);
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    request = new HttpRequestMessage(HttpMethod.Patch, endpoint);
                    response = await _httpClient.SendAsync(request);
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<bool> PatchAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PatchAsJsonAsync(endpoint, data);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.PatchAsJsonAsync(endpoint, data);
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching {Endpoint}", endpoint);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.DeleteAsync(endpoint);
                }
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {Endpoint}", endpoint);
            return false;
        }
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh",
                new { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
            if (result == null)
                return false;

            await _localStorage.SetItemAsync("accessToken", result.AccessToken);
            await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }
}

public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
