using AssetHierarchyWebAPI.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace AssetHierarchyWebAPI.Models
{
    public class AppUser : IdentityUser
    {
        public int TokenVersion { get; set; } = 0;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
