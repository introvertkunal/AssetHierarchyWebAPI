using AssetHierarchyWebAPI.Application.DTOs.Auth;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetHierarchyWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AuthController(IAuthService authService,
                              UserManager<AppUser> userManager,
                              SignInManager<AppUser> signInManager)
        {
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success) return BadRequest(result.Error);
            return Ok(result.Result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.LoginAsync(request, ip);

            if (!result.Success)
            {
                if (result.Error == "Admin already logged in elsewhere.")
                    return Conflict(result.Error);
                else
                    return Unauthorized(result.Error);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            };

            Response.Cookies.Append("access_token", result.Result!.AccessToken!, cookieOptions);
            Response.Cookies.Append("refresh_token", result.Result.RefreshToken!, cookieOptions);

            return Ok(result.Result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refresh = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refresh)) return Unauthorized("No refresh token.");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _authService.RefreshAsync(refresh, ip);
            if (!result.Success) return Unauthorized(result.Error);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(1)
            };

            Response.Cookies.Append("access_token", result.Result!.AccessToken!, cookieOptions);
            Response.Cookies.Append("refresh_token", result.Result.RefreshToken!, cookieOptions);

            return Ok(result.Result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refresh = Request.Cookies["refresh_token"];
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await _authService.LogoutAsync(refresh, userId, ip);
            if (!result.Success) return BadRequest(result.Error);

            Response.Cookies.Delete("refresh_token");
            Response.Cookies.Delete("access_token");

            await _signInManager.SignOutAsync();
            return Ok("Logged out successfully.");
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _authService.GetCurrentUserAsync(userId);
            return Ok(result);
        }

        // External Login
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

                // Generate JWT and set cookies
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var authService = HttpContext.RequestServices.GetRequiredService<IAuthService>();

                var jwt = await _authService.ExternalLoginAsync(user, ip);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(1)
                };

                Response.Cookies.Append("access_token", jwt.AccessToken!, cookieOptions);
                Response.Cookies.Append("refresh_token", jwt.RefreshToken!, cookieOptions);

                return Redirect($"{returnUrl}?success=true");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
