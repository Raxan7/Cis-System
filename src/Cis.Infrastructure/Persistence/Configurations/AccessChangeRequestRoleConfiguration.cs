using Cis.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class AccessChangeRequestRoleConfiguration : IEntityTypeConfiguration<AccessChangeRequestRole>
{
    public void Configure(EntityTypeBuilder<AccessChangeRequestRole> builder)
    {
        builder.ToTable("access_change_request_roles", "identity");

        builder.HasKey(requestRole => requestRole.Id);

        builder.HasOne(requestRole => requestRole.AccessChangeRequest)
            .WithMany(request => request.Roles)
            .HasForeignKey(requestRole => requestRole.AccessChangeRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(requestRole => requestRole.Role)
            .WithMany()
            .HasForeignKey(requestRole => requestRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(requestRole => new { requestRole.AccessChangeRequestId, requestRole.RoleId }).IsUnique();
        builder.HasIndex(requestRole => requestRole.RoleId);
    }
}
