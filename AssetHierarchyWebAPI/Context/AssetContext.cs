using AssetHierarchyWebAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyWebAPI.Context
{
    public class AssetContext : IdentityDbContext<AppUser>
    {
        public AssetContext(DbContextOptions<AssetContext> options) : base(options) { }

        public DbSet<AssetNode> AssetHierarchy { get; set; }
        public DbSet<AssetSignals> AssetSignal { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AssetNode>()
                .HasOne(a => a.Parent)
                .WithMany(a => a.Children)
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<AssetSignals>()
                .HasOne(s => s.AssetNode)
                .WithMany(a => a.Signals)
                .HasForeignKey(s => s.AssetNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
