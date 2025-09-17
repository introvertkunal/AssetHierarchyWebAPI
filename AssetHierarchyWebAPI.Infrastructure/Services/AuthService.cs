using AssetHierarchyWebAPI.Application.DTOs.Auth;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;
        private readonly AssetContext _db;

        public AuthService(UserManager<AppUser> userManager, IConfiguration config, AssetContext db)
        {
            _userManager = userManager;
            _config = config;
            _db = db;
        }

        public async Task<(bool Success, object Error, AuthResponse? Result)> RegisterAsync(RegisterRequest request)
        {
            var user = new AppUser
            {
                UserName = request.UserName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return (false, result.Errors, null);

            await _userManager.AddToRoleAsync(user, "User");

            return (true, string.Empty, new AuthResponse
            {
                UserName = user.UserName,
                Roles = new List<string> { "User" }
            });
        }

        public async Task<(bool Success, string Error, AuthResponse? Result)> LoginAsync(LoginRequest request, string ipAddress)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null) return (false, "Invalid Username.", null);

            var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValid) return (false, "Invalid Password.", null);

            var roles = await _userManager.GetRolesAsync(user);

            // Only one active Admin session
            if (roles.Contains("Admin"))
            {
                var hasActive = await _db.RefreshTokens
                    .AnyAsync(rt => rt.AppUserId == user.Id && rt.Revoked == null && rt.Expires > DateTime.UtcNow);

                if (hasActive) return (false, "Admin already logged in elsewhere.", null);
            }

            var response = await GenerateJwtAsync(user, ipAddress, true);
            return (true, string.Empty, response);
        }

        public async Task<(bool Success, string Error, AuthResponse? Result)> RefreshAsync(string refreshToken, string ipAddress)
        {
            var tokenEntity = await _db.RefreshTokens.Include(rt => rt.AppUser)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenEntity == null || !tokenEntity.IsActive)
                return (false, "Invalid or expired refresh token.", null);

            var user = tokenEntity.AppUser;

            tokenEntity.Revoked = DateTime.UtcNow;
            tokenEntity.RevokedByIp = ipAddress;

            var newRefresh = new RefreshToken
            {
                Token = GenerateRandomToken(),
                Expires = DateTime.UtcNow.AddDays(1),
                CreatedByIp = ipAddress,
                AppUserId = user.Id,
                Created = DateTime.UtcNow
            };

            tokenEntity.ReplacedByToken = newRefresh.Token;
            _db.RefreshTokens.Add(newRefresh);
            await _db.SaveChangesAsync();

            var response = await GenerateJwtAsync(user, ipAddress, false, newRefresh.Token);
            return (true, string.Empty, response);
        }

        public async Task<(bool Success, string Error)> LogoutAsync(string refreshToken, string userId, string ipAddress)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenEntity = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
                if (tokenEntity != null && tokenEntity.IsActive)
                {
                    tokenEntity.Revoked = DateTime.UtcNow;
                    tokenEntity.RevokedByIp = ipAddress;
                }
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.TokenVersion++;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _db.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<AuthResponse> ExternalLoginAsync(AppUser user, string ipAddress)
        {
           
            var response = await GenerateJwtAsync(user, ipAddress, true);
            return response;
        }


        public async Task<AuthResponse?> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new AuthResponse { UserName = user.UserName!, Roles = roles.ToList() };
        }

        private async Task<AuthResponse> GenerateJwtAsync(AppUser user, string ipAddress, bool createRefresh, string? refreshToken = null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("tv", user.TokenVersion.ToString())
            };

            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

            // Only create refresh if true
            if (createRefresh && refreshToken == null)
            {
                var refresh = new RefreshToken
                {
                    Token = GenerateRandomToken(),
                    Expires = DateTime.UtcNow.AddDays(1),
                    CreatedByIp = ipAddress,
                    AppUserId = user.Id,
                    Created = DateTime.UtcNow
                };
                _db.RefreshTokens.Add(refresh);
                await _db.SaveChangesAsync();
                refreshToken = refresh.Token;
            }

            return new AuthResponse
            {
                UserName = user.UserName!,
                Roles = roles.ToList(),
                AccessToken = tokenString,
                RefreshToken = refreshToken
            };
        }

        private string GenerateRandomToken(int size = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(bytes);
        }
    }
}
