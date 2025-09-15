
using Microsoft.AspNetCore.Identity;

namespace AssetHierarchyWebAPI.Domain.Entities.Auth
{
    public class AppUser : IdentityUser
    {
        public int TokenVersion { get; set; } = 0;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
