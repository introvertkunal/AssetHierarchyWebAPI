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

            // AssetNode self-reference
            modelBuilder.Entity<AssetNode>()
                .HasOne(a => a.Parent)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.ClientCascade);

            // AssetSignals -> AssetNode
            modelBuilder.Entity<AssetSignals>()
                .HasOne(s => s.AssetNode)
                .WithMany(a => a.Signals)
                .HasForeignKey(s => s.AssetNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            // RefreshToken -> AppUser
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        }
    }
}
