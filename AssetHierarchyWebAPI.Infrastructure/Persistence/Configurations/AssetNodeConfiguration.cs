using AssetHierarchyWebAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetHierarchyWebAPI.Infrastructure.Persistence.Configurations
{
    public class AssetNodeConfiguration : IEntityTypeConfiguration<AssetNode>
    {
        public void Configure(EntityTypeBuilder<AssetNode> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(a => a.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasOne(a => a.Parent)
                   .WithMany(p => p.Children)
                   .HasForeignKey(a => a.ParentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.Signals)
                   .WithOne(s => s.AssetNode)
                   .HasForeignKey(s => s.AssetNodeId);
        }
    }
}
