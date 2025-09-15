using AssetHierarchyWebAPI.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace AssetHierarchyWebAPI.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                   .IsRequired();

            builder.Property(rt => rt.Expires)
                   .IsRequired();

            builder.Property(rt => rt.Created)
                   .IsRequired();

            builder.HasOne(rt => rt.AppUser)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(rt => rt.AppUserId);
        }
    }
}
