namespace AssetHierarchyWebAPI.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string UserName { get; set; } = default!;
        public List<string> Roles { get; set; } = new();
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
