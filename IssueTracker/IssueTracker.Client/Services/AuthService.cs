using System.Net.Http.Headers;
using System.Net.Http.Json;
//using IssueTracker.Client.DTOs;
using IssueTracker.Core.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace IssueTracker.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _js;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public AuthService(
        HttpClient httpClient,
        IJSRuntime js,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _js = js;
        _authStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        // 1. Invio delle credenziali all'endpoint del backend
        var loginModel = new LoginDto
        {
            Username = username,
            Password = password
        };
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        // 2. Legge i token restituiti dall'API
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result == null || string.IsNullOrWhiteSpace(result.Token))
        {
            return false;
        }

        // 3. Salva sia l'Access Token che il Refresh Token nel LocalStorage del browser
        await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
        if (!string.IsNullOrWhiteSpace(result.RefreshToken))
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);
        }

        // 4. Aggiorna l'header di default per l'HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

        // 5. Notifica Blazor che l'utente è ora autenticato
        _authStateProvider.NotifyUserAuthentication(result.Token);

        return true;
    }

    public async Task LogoutAsync()
    {
        // 1. Rimuove i token salvati nel LocalStorage
        await _js.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _js.InvokeVoidAsync("localStorage.removeItem", "refreshToken");

        // 2. Rimuove l'header Authorization dall'HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = null;

        // 3. Notifica a Blazor che l'utente si è scollegato
        _authStateProvider.NotifyUserLogout();
    }

    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerDto);
        if (!response.IsSuccessStatusCode) {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Registrazione fallita: {errorContent}");
            return false;
        }
        await LoginAsync(registerDto.Username, registerDto.Password);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Login automatico fallito dopo la registrazione: {errorContent}");
            return false;
        }
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> RefreshTokenAsync()
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");
        var refreshToken = await _js.InvokeAsync<string>("localStorage.getItem", "refreshToken");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refreshToken))
        {
            await LogoutAsync();
            return null;
        }

        var refreshModel = new RefreshTokenRequestDto{ AccessToken = token, RefreshToken = refreshToken };
        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", refreshModel);

        if (!response.IsSuccessStatusCode)
        {
            await LogoutAsync();
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result == null || string.IsNullOrWhiteSpace(result.Token))
        {
            await LogoutAsync();
            return null;
        }

        // Salva i nuovi token ricevuti
        await _js.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
        if (!string.IsNullOrWhiteSpace(result.RefreshToken))
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);
        }

        _authStateProvider.NotifyUserAuthentication(result.Token);
        return result.Token;
    }
}