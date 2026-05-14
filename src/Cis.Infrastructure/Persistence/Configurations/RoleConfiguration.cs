using Cis.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(role => role.NormalizedName).IsUnique();
        builder.HasIndex(role => role.IsPrivileged);

        builder.Metadata.FindNavigation(nameof(Role.Permissions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
