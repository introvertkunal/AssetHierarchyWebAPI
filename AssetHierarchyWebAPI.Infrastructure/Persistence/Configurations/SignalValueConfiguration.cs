using AssetHierarchyWebAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Infrastructure.Persistence.Configurations
{
    public class SignalValueConfiguration : IEntityTypeConfiguration<SignalValue>
    {
        public void Configure(EntityTypeBuilder<SignalValue> builder)
        {
            builder.HasKey(s => s.ValueId);

            builder.Property(s => s.ValueId)
                   .ValueGeneratedOnAdd();

            builder.Property(s => s.RecordedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(s => s.SignalId)
                   .IsRequired();

            builder.HasOne(s => s.AssetSignal)
                   .WithMany(n => n.SignalValues)
                   .HasForeignKey(s => s.SignalId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
