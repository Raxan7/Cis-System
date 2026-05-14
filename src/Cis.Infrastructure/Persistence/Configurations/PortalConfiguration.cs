using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Portal;
using Cis.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class PortalUserProfileConfiguration : IEntityTypeConfiguration<PortalUserProfile>
{
    public void Configure(EntityTypeBuilder<PortalUserProfile> builder)
    {
        builder.ToTable("portal_user_profiles", "portal");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.Email).HasMaxLength(320).IsRequired();
        builder.Property(profile => profile.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(profile => profile.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(profile => profile.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(profile => profile.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.UserId).IsUnique();
        builder.HasIndex(profile => profile.InvestorId).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class PortalSessionConfiguration : IEntityTypeConfiguration<PortalSession>
{
    public void Configure(EntityTypeBuilder<PortalSession> builder)
    {
        builder.ToTable("portal_sessions", "portal");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.IpAddress).HasMaxLength(100);
        builder.Property(session => session.UserAgent).HasMaxLength(500);
        builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(session => session.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(session => session.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(session => new { session.UserId, session.Status, session.LastSeenAtUtc });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class PortalActivityLogConfiguration : IEntityTypeConfiguration<PortalActivityLog>
{
    public void Configure(EntityTypeBuilder<PortalActivityLog> builder)
    {
        builder.ToTable("portal_activity_logs", "portal");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.ActivityType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(log => log.Summary).HasMaxLength(500).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(100);
        builder.Property(log => log.EntityId).HasMaxLength(100);
        builder.Property(log => log.IpAddress).HasMaxLength(100);
        builder.Property(log => log.CorrelationId).HasMaxLength(100);
        builder.HasOne<User>().WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(log => log.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PortalSession>().WithMany().HasForeignKey(log => log.PortalSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(log => new { log.InvestorId, log.OccurredAtUtc });
        builder.HasIndex(log => new { log.UserId, log.OccurredAtUtc });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class DigitalServiceRequestConfiguration : IEntityTypeConfiguration<DigitalServiceRequest>
{
    public void Configure(EntityTypeBuilder<DigitalServiceRequest> builder)
    {
        builder.ToTable("digital_service_requests", "portal");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.RequestType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(request => request.RequestPayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(request => request.SubmittedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(request => request.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(request => request.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowInstance>().WithMany().HasForeignKey(request => request.WorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(request => new { request.InvestorId, request.RequestType, request.Status });
        builder.HasIndex(request => request.WorkflowId);
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class PortalDocumentDownloadConfiguration : IEntityTypeConfiguration<PortalDocumentDownload>
{
    public void Configure(EntityTypeBuilder<PortalDocumentDownload> builder)
    {
        builder.ToTable("portal_document_downloads", "portal");
        builder.HasKey(download => download.Id);
        builder.Property(download => download.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(download => download.DocumentReference).HasMaxLength(200).IsRequired();
        builder.Property(download => download.IpAddress).HasMaxLength(100);
        builder.HasOne<User>().WithMany().HasForeignKey(download => download.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(download => download.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(download => new { download.InvestorId, download.DocumentType, download.DownloadedAtUtc });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class InvestorNoticeConfiguration : IEntityTypeConfiguration<InvestorNotice>
{
    public void Configure(EntityTypeBuilder<InvestorNotice> builder)
    {
        builder.ToTable("investor_notices", "portal");
        builder.HasKey(notice => notice.Id);
        builder.Property(notice => notice.Title).HasMaxLength(200).IsRequired();
        builder.Property(notice => notice.Body).HasMaxLength(4000).IsRequired();
        builder.Property(notice => notice.PublishedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(notice => notice.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Investor>().WithMany().HasForeignKey(notice => notice.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(notice => new { notice.InvestorId, notice.Status, notice.PublishedDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class PortalMfaSettingConfiguration : IEntityTypeConfiguration<PortalMfaSetting>
{
    public void Configure(EntityTypeBuilder<PortalMfaSetting> builder)
    {
        builder.ToTable("portal_mfa_settings", "portal");
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.UpdatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(setting => setting.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(setting => setting.UserId).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}
