using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Domain.Entities.Auth;
using AssetHierarchyWebAPI.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Infrastructure.Persistence
{
    public class AssetContext : IdentityDbContext<AppUser>
    {
        public AssetContext(DbContextOptions<AssetContext> options) : base(options) { }

        public DbSet<AssetNode> AssetHierarchy { get; set; }
        public DbSet<AssetSignals> AssetSignal { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfiguration(new AssetNodeConfiguration());
            modelBuilder.ApplyConfiguration(new AssetSignalConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        }
    }
}