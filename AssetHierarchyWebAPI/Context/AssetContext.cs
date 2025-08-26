using AssetHierarchyWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace AssetHierarchyWebAPI.Context
{
    public class AssetContext : DbContext
    {
        public AssetContext(DbContextOptions<AssetContext> options) : base(options) { }

        public DbSet<AssetNode> AssetHierarchy { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Declare Constraints
            modelBuilder.Entity<AssetNode>()
                .HasMany(a => a.Children)
                .WithOne()
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Keep Name Unique
            modelBuilder.Entity<AssetNode>()
                .HasIndex(a => a.Name)
                .IsUnique();
        }


    }

}
