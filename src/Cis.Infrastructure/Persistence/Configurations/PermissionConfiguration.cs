using Cis.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", "identity");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(permission => permission.Module)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(permission => permission.Code).IsUnique();
        builder.HasIndex(permission => permission.Module);
    }
}
