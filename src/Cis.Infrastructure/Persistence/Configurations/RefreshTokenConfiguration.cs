using Cis.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "identity");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.CreatedByIpAddress)
            .HasMaxLength(100);

        builder.Property(refreshToken => refreshToken.RevokedByIpAddress)
            .HasMaxLength(100);

        builder.Property(refreshToken => refreshToken.ReplacedByTokenHash)
            .HasMaxLength(200);

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();
        builder.HasIndex(refreshToken => new { refreshToken.UserId, refreshToken.ExpiresAtUtc });
    }
}
