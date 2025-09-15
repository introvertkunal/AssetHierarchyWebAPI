using AssetHierarchyWebAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetHierarchyWebAPI.Infrastructure.Persistence.Configurations
{
    public class AssetSignalConfiguration : IEntityTypeConfiguration<AssetSignals>
    {
        public void Configure(EntityTypeBuilder<AssetSignals> builder)
        {
            builder.HasKey(s => s.SignalId);

            builder.Property(s => s.SignalId)
                   .ValueGeneratedOnAdd();

            builder.Property(s => s.SignalName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.SignalType)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(s => s.Description)
                   .HasMaxLength(500);

            builder.HasOne(s => s.AssetNode)
                   .WithMany(n => n.Signals)
                   .HasForeignKey(s => s.AssetNodeId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
