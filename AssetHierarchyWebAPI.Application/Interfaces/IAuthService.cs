using AssetHierarchyWebAPI.Application.DTOs.Auth;
using AssetHierarchyWebAPI.Domain.Entities.Auth;

namespace AssetHierarchyWebAPI.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, object Error, AuthResponse? Result)> RegisterAsync(RegisterRequest request);
        Task<(bool Success, string Error, AuthResponse? Result)> LoginAsync(LoginRequest request, string ipAddress);
        Task<(bool Success, string Error, AuthResponse? Result)> RefreshAsync(string refreshToken, string ipAddress);
        Task<(bool Success, string Error)> LogoutAsync(string refreshToken, string userId, string ipAddress);
        Task<AuthResponse> ExternalLoginAsync(AppUser user, string ipAddress);
        Task<AuthResponse?> GetCurrentUserAsync(string userId);
    }
}
