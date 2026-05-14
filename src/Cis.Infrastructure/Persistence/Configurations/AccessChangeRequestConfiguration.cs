using Cis.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class AccessChangeRequestConfiguration : IEntityTypeConfiguration<AccessChangeRequest>
{
    public void Configure(EntityTypeBuilder<AccessChangeRequest> builder)
    {
        builder.ToTable("access_change_requests", "identity");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.RequestNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(request => request.ChangeType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(request => request.RequestedByUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(request => request.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(request => request.DecisionByUserId)
            .HasMaxLength(200);

        builder.Property(request => request.DecisionReason)
            .HasMaxLength(1000);

        builder.OwnsOne(request => request.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });

        builder.HasOne(request => request.TargetUser)
            .WithMany()
            .HasForeignKey(request => request.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => request.RequestNumber).IsUnique();
        builder.HasIndex(request => new { request.TargetUserId, request.Status });
        builder.HasIndex(request => new { request.RequestedByUserId, request.Status });

        builder.Metadata.FindNavigation(nameof(AccessChangeRequest.Roles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
