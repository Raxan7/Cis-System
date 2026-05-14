using Cis.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class IntegrationEndpointConfiguration : IEntityTypeConfiguration<IntegrationEndpoint>
{
    public void Configure(EntityTypeBuilder<IntegrationEndpoint> builder)
    {
        builder.ToTable("integration_endpoints", "integrations");
        builder.HasKey(endpoint => endpoint.Id);
        builder.Property(endpoint => endpoint.IntegrationType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(endpoint => endpoint.Code).HasMaxLength(80).IsRequired();
        builder.Property(endpoint => endpoint.Name).HasMaxLength(200).IsRequired();
        builder.Property(endpoint => endpoint.BaseAddress).HasMaxLength(500).IsRequired();
        builder.HasIndex(endpoint => endpoint.Code).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationEndpoint> builder)
    {
        builder.OwnsOne(endpoint => endpoint.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class IntegrationCredentialReferenceConfiguration : IEntityTypeConfiguration<IntegrationCredentialReference>
{
    public void Configure(EntityTypeBuilder<IntegrationCredentialReference> builder)
    {
        builder.ToTable("integration_credential_references", "integrations");
        builder.HasKey(credential => credential.Id);
        builder.Property(credential => credential.CredentialName).HasMaxLength(100).IsRequired();
        builder.Property(credential => credential.SecretReference).HasMaxLength(500).IsRequired();
        builder.HasOne<IntegrationEndpoint>().WithMany().HasForeignKey(credential => credential.IntegrationEndpointId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(credential => new { credential.IntegrationEndpointId, credential.CredentialName }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationCredentialReference> builder)
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class IntegrationMessageConfiguration : IEntityTypeConfiguration<IntegrationMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationMessage> builder)
    {
        builder.ToTable("integration_messages", "integrations");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.IntegrationType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(message => message.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(message => message.EndpointCode).HasMaxLength(80).IsRequired();
        builder.Property(message => message.ExternalReference).HasMaxLength(120).IsRequired();
        builder.Property(message => message.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(message => message.PayloadStorageReference).HasMaxLength(1000);
        builder.Property(message => message.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(message => new { message.EndpointCode, message.ExternalReference });
        builder.HasIndex(message => new { message.IntegrationType, message.Status });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationMessage> builder)
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class IntegrationIngestionRunConfiguration : IEntityTypeConfiguration<IntegrationIngestionRun>
{
    public void Configure(EntityTypeBuilder<IntegrationIngestionRun> builder)
    {
        builder.ToTable("integration_ingestion_runs", "integrations");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.IntegrationType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.AdapterName).HasMaxLength(100).IsRequired();
        builder.Property(run => run.FileName).HasMaxLength(200).IsRequired();
        builder.Property(run => run.SourceHash).HasMaxLength(128).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasOne<IntegrationMessage>().WithMany().HasForeignKey(run => run.IntegrationMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => new { run.IntegrationType, run.CompletedAtUtc });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationIngestionRun> builder)
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class IntegrationDeliveryAttemptConfiguration : IEntityTypeConfiguration<IntegrationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<IntegrationDeliveryAttempt> builder)
    {
        builder.ToTable("integration_delivery_attempts", "integrations");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(attempt => attempt.ResponseCode).HasMaxLength(80);
        builder.Property(attempt => attempt.ResponseMessage).HasMaxLength(1000);
        builder.HasOne<IntegrationMessage>().WithMany().HasForeignKey(attempt => attempt.IntegrationMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attempt => new { attempt.IntegrationMessageId, attempt.AttemptNumber }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationDeliveryAttempt> builder)
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class IntegrationErrorConfiguration : IEntityTypeConfiguration<IntegrationError>
{
    public void Configure(EntityTypeBuilder<IntegrationError> builder)
    {
        builder.ToTable("integration_errors", "integrations");
        builder.HasKey(error => error.Id);
        builder.Property(error => error.IntegrationType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(error => error.ErrorCode).HasMaxLength(80).IsRequired();
        builder.Property(error => error.ErrorMessage).HasMaxLength(1000).IsRequired();
        builder.Property(error => error.SourceReference).HasMaxLength(200);
        builder.HasOne<IntegrationMessage>().WithMany().HasForeignKey(error => error.IntegrationMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(error => new { error.IntegrationType, error.OccurredAtUtc });
        builder.HasIndex(error => error.Retryable);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationError> builder)
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class IntegrationIdempotencyKeyConfiguration : IEntityTypeConfiguration<IntegrationIdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IntegrationIdempotencyKey> builder)
    {
        builder.ToTable("integration_idempotency_keys", "integrations");
        builder.HasKey(key => key.Id);
        builder.Property(key => key.Key).HasMaxLength(200).IsRequired();
        builder.Property(key => key.EndpointCode).HasMaxLength(80).IsRequired();
        builder.Property(key => key.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(key => key.ResponseJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne<IntegrationMessage>().WithMany().HasForeignKey(key => key.IntegrationMessageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(key => new { key.EndpointCode, key.Key }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<IntegrationIdempotencyKey> builder)
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}
