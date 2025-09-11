using System;

namespace AssetHierarchyWebAPI.Models.Auth
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsActive => Revoked == null && !IsExpired;

        // FK to AppUser
        public string AppUserId { get; set; } = null!;
        public AppUser AppUser { get; set; } = null!;
    }
}

