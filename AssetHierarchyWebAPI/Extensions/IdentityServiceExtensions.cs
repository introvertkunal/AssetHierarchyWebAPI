using System.Text;
using AssetHierarchyWebAPI.Context;
using AssetHierarchyWebAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AssetHierarchyWebAPI.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AssetContext>(options =>
                options.UseSqlServer(config.GetConnectionString("AssetConnStr")));

                        services.AddIdentity<AppUser, IdentityRole>(options =>
                        {
                            options.User.RequireUniqueEmail = true;
                            options.Password.RequiredLength = 8;
                            options.Password.RequireDigit = true;
                            options.Password.RequireUppercase = true;
                            options.Password.RequireLowercase = true;
                            options.Password.RequireNonAlphanumeric = false;
                        })
                        .AddEntityFrameworkStores<AssetContext>()
                        .AddDefaultTokenProviders();

                        var jwtSection = config.GetSection("Jwt");
                        var key = jwtSection["Key"]!;
                        var issuer = jwtSection["Issuer"]!;
                        var audience = jwtSection["Audience"]!;

                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        })
             .AddJwtBearer(options =>
             {
                 options.RequireHttpsMetadata = false;
                 options.SaveToken = true;
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                     ValidateIssuer = true,
                     ValidIssuer = issuer,
                     ValidateAudience = true,
                     ValidAudience = audience,
                     ValidateLifetime = true,
                     ClockSkew = TimeSpan.Zero
                 };

                 options.Events = new JwtBearerEvents
                 {
                     OnMessageReceived = context =>
                     {
                         // prefer access_token cookie then fallback to Authorization header
                         var tokenFromCookie = context.Request.Cookies["access_token"] ?? context.Request.Cookies["auth_token"];
                         if (!string.IsNullOrEmpty(tokenFromCookie))
                         {
                             context.Token = tokenFromCookie;
                         }
                         return Task.CompletedTask;
                     },
                     OnTokenValidated = async context =>
                     {
                         var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                         var userId = context.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                         var tvClaim = context.Principal.FindFirst("tv")?.Value;

                         if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tvClaim))
                         {
                             context.Fail("Missing claims.");
                             return;
                         }

                         var user = await userManager.FindByIdAsync(userId);
                         if (user == null)
                         {
                             context.Fail("User no longer exists.");
                             return;
                         }

                         if (!int.TryParse(tvClaim, out var tv) || tv != user.TokenVersion)
                         {
                             context.Fail("Token has been revoked.");
                             return;
                         }
                     },
                     OnAuthenticationFailed = context =>
                     {
                         // optional: log
                         return Task.CompletedTask;
                     }
                 };
             })

            .AddGoogle("Google", options =>
            {
                options.ClientId = config["Authentication:Google:ClientId"]!;
                options.ClientSecret = config["Authentication:Google:ClientSecret"]!;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddGitHub("GitHub", options =>
            {
                options.ClientId = config["Authentication:GitHub:ClientId"]!;
                options.ClientSecret = config["Authentication:GitHub:ClientSecret"]!;
                options.Scope.Add("user:email");
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", p => p.RequireRole("User", "Admin"));
            });

            return services;
        }
    }
}
