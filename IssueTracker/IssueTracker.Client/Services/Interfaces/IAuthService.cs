using IssueTracker.Core.DTOs;

namespace IssueTracker.Client.Services.Interfaces;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();

    Task<bool> RegisterAsync(RegisterDto registerDto);
    Task<string?> RefreshTokenAsync();
}