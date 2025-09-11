using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Models;
using AssetHierarchyWebAPI.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly AssetContext _db;

        public AuthController(UserManager<AppUser> userManager,
                              SignInManager<AppUser> signInManager,
                              IConfiguration config,
                              AssetContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest("Invalid request.");

                var user = new AppUser
                {
                    UserName = request.UserName,
                    Email = request.Email
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded) return BadRequest(result.Errors);

                await _userManager.AddToRoleAsync(user, "User");
                return Ok("User registered successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(request.UserName);
                if (user == null) return Unauthorized("Invalid Username.");

                var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!isValid) return Unauthorized("Invalid Password.");

                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");

                if (isAdmin)
                {
                    Console.WriteLine("Dekh idhar tak chal raha hai");
                var hasActive = await _db.RefreshTokens
                                            .AnyAsync(rt => rt.AppUserId == user.Id
                                                         && rt.Revoked == null
                                                         && rt.Expires > DateTime.UtcNow);


                Console.WriteLine($"Active ki value yeh hai: {hasActive}");
                    if (hasActive)
                        return Conflict("Admin already logged in from another browser/device.");
                }

                var result = await GenerateJwt(user, createRefreshToken: true);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                var refreshToken = Request.Cookies["refresh_token"];
                if (string.IsNullOrEmpty(refreshToken))
                    return Unauthorized("No refresh token.");

                var tokenEntity = await _db.RefreshTokens.Include(rt => rt.AppUser)
                                          .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (tokenEntity == null || !tokenEntity.IsActive)
                    return Unauthorized("Invalid or expired refresh token.");

                var user = tokenEntity.AppUser;

                // rotate token
                tokenEntity.Revoked = DateTime.UtcNow;
                tokenEntity.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var newRefresh = new RefreshToken
                {
                    Token = GenerateRandomToken(64),
                    Expires = DateTime.UtcNow.AddDays(1),
                    CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    AppUserId = user.Id,
                    Created = DateTime.UtcNow
                };

                tokenEntity.ReplacedByToken = newRefresh.Token;

                _db.RefreshTokens.Add(newRefresh);
                await _db.SaveChangesAsync();

                Response.Cookies.Append("refresh_token", newRefresh.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = newRefresh.Expires
                });

                var jwt = await GenerateJwt(user, createRefreshToken: false);
                return Ok(jwt);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var refresh = Request.Cookies["refresh_token"];
                if (!string.IsNullOrEmpty(refresh))
                {
                    var tokenEntity = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refresh);
                    if (tokenEntity != null && tokenEntity.IsActive)
                    {
                        tokenEntity.Revoked = DateTime.UtcNow;
                        tokenEntity.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                    }
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        user.TokenVersion += 1;
                        await _userManager.UpdateAsync(user);
                    }
                }

                Response.Cookies.Delete("refresh_token");
                Response.Cookies.Delete("access_token");
                Response.Cookies.Delete("auth_token");

                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                return Ok("Logged out successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("externallogin")]
        public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = "/")
        {
            try
            {
                var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { returnUrl });
                var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return Challenge(props, provider);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("externallogincallback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        {
            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null) return Redirect("/login?error=external");

                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user == null)
                {
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                                ?? $"{info.LoginProvider}_{info.ProviderKey}@temp.local";

                    user = await _userManager.FindByEmailAsync(email);

                    string safeUserName = info.LoginProvider switch
                    {
                        "Google" => email,
                        "GitHub" => info.Principal.FindFirstValue(ClaimTypes.Name)
                                    ?? info.Principal.FindFirstValue("urn:github:login")
                                    ?? $"github_{info.ProviderKey}",
                        _ => email.Replace(" ", "_")
                    };

                    if (user == null)
                    {
                        user = new AppUser
                        {
                            UserName = safeUserName,
                            Email = email,
                            EmailConfirmed = true
                        };

                        var result = await _userManager.CreateAsync(user);
                        if (!result.Succeeded) return BadRequest(result.Errors);

                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    await _userManager.AddLoginAsync(user, info);
                }

                foreach (var token in info.AuthenticationTokens)
                    await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, token.Name, token.Value);

                await GenerateJwt(user, createRefreshToken: true);

                return Redirect($"{returnUrl}?success=true");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized("User ID not found.");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return Unauthorized("User not found.");

                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new { userName = user.UserName, roles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string GenerateRandomToken(int size = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(bytes);
        }

        private async Task<object> GenerateJwt(AppUser user, bool createRefreshToken = true)
        {
            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("tv", user.TokenVersion.ToString())
                };

                foreach (var role in roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

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

                Response.Cookies.Append("access_token", tokenString, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(10)
                });

                if (createRefreshToken)
                {
                    var refreshToken = new RefreshToken
                    {
                        Token = GenerateRandomToken(64),
                        Expires = DateTime.UtcNow.AddDays(1),
                        CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        AppUserId = user.Id,
                        Created = DateTime.UtcNow
                    };

                    _db.RefreshTokens.Add(refreshToken);
                    await _db.SaveChangesAsync();

                    Response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = refreshToken.Expires
                    });
                }

                return new
                {
                    userName = user.UserName,
                    roles,
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"JWT generation failed: {ex.Message}");
            }
        }
    }
}
