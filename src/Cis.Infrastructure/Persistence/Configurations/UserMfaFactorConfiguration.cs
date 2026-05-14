using Cis.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class UserMfaFactorConfiguration : IEntityTypeConfiguration<UserMfaFactor>
{
    public void Configure(EntityTypeBuilder<UserMfaFactor> builder)
    {
        builder.ToTable("user_mfa_factors", "identity");

        builder.HasKey(factor => factor.Id);

        builder.Property(factor => factor.FactorType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(factor => factor.SecretHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(factor => factor.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(factor => factor.User)
            .WithMany(user => user.MfaFactors)
            .HasForeignKey(factor => factor.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(factor => new { factor.UserId, factor.FactorType, factor.Status });
    }
}
