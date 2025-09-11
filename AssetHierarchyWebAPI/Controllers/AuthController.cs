using AssetHierarchyWebAPI.Models;
using AssetHierarchyWebAPI.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request.");

            var user = new AppUser
            {
                UserName = request.UserName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User");
            return Ok("User registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null) return Unauthorized("Invalid Username.");

            var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValid) return Unauthorized("Invalid Password.");

            return Ok(await GenerateJwt(user));
        }

        [HttpGet("externallogin")]
        public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string returnUrl = "/")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { returnUrl });
            var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(props, provider);
        }

        [HttpGet("externallogincallback")]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return Redirect("/login?error=external");

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user == null)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                // GitHub can hide email -> fallback
                if (string.IsNullOrEmpty(email))
                {
                    email = $"{info.LoginProvider}_{info.ProviderKey}@temp.local";
                }

                user = await _userManager.FindByEmailAsync(email);

                // Decide username based on provider
                string safeUserName;
                if (info.LoginProvider == "Google")
                {
                    // Use Email as username
                    safeUserName = email;
                }
                else if (info.LoginProvider == "GitHub")
                {
                    // Prefer GitHub username claim
                    safeUserName = info.Principal.FindFirstValue(ClaimTypes.Name)
                                    ?? info.Principal.FindFirstValue("urn:github:login")
                                    ?? $"github_{info.ProviderKey}";
                }
                else
                {
                    // Default fallback
                    safeUserName = email.Replace(" ", "_");
                }

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

            // Save external tokens
            foreach (var token in info.AuthenticationTokens)
            {
                await _userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, token.Name, token.Value);
            }

            await GenerateJwt(user);

            return Redirect($"{returnUrl}?success=true");
        }


        private async Task<object> GenerateJwt(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            Response.Cookies.Append("auth_token", tokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(2)
            });

            return new
            {
                userName = user.UserName,
                roles
                //token = tokenString
            };
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("auth_token");
            return Ok("Logged out successfully.");
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("User ID not found.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                userName = user.UserName,
                roles
            });
        }
    }
}
