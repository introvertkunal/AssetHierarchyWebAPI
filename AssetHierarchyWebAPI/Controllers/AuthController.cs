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
        private readonly IConfiguration _config;

        public AuthController(UserManager<AppUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        // Public registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during registration: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(request.UserName);
                if (user == null)
                    return Unauthorized("Invalid Username.");

                var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!isValid)
                    return Unauthorized("Invalid Password.");

                var userRoles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var role in userRoles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // Set HTTP-only cookie
                Response.Cookies.Append("auth_token", tokenString, new CookieOptions
                {
                    HttpOnly = true, 
                    Secure = true,  
                    SameSite = SameSiteMode.Strict, // Prevent CSRF
                    Expires = DateTime.UtcNow.AddHours(2) 
                });

               
                return Ok(new
                {
                    userName = user.UserName,
                    roles = userRoles
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during login: {ex.Message}");
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                // Delete the auth_token cookie
                Response.Cookies.Delete("auth_token");
                return Ok("Logged out successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during logout: {ex.Message}");
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Get the user ID from the JWT claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token.");

                // Fetch the user from the database
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Unauthorized("User not found.");

                // Get the user's roles
                var userRoles = await _userManager.GetRolesAsync(user);

                // Return user information
                return Ok(new
                {
                    userName = user.UserName,
                    roles = userRoles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while fetching user data: {ex.Message}");
            }
        }
    }
}
