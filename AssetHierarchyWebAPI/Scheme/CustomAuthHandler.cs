using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AssetHierarchyWebAPI.Scheme
{
    public class CustomAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public CustomAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if(!Request.Headers.TryGetValue("Api-key", out var apiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing API Key"));
            }

            ClaimsIdentity identity = null;

            if(apiKey == "admin-key")
            {
                identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "Admin-User"),
                    new Claim(ClaimTypes.Role, "Admin")
                }, Scheme.Name);
            }
            else if(apiKey == "user-key")
            {
                identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "Normal-User"),
                    new Claim(ClaimTypes.Role, "User")
                }, Scheme.Name);
            }
            else
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
            }

            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
            
        }
    }
}
