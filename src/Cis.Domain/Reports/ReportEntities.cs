using Cis.Domain.Common;

namespace Cis.Domain.Reports;

public sealed class ReportDefinition : AuditableAggregateRoot
{
    private readonly List<ReportOwnerMatrix> _owners = [];

    private ReportDefinition()
    {
    }

    private ReportDefinition(string code, string name, string category, ReportFrequency frequency, string primaryUsers, string requiredPermission, string createdByUserId, DateTime createdAtUtc)
    {
        Code = ReportValidation.Required(code, nameof(code), 50).ToUpperInvariant();
        Name = ReportValidation.Required(name, nameof(name), 200);
        Category = ReportValidation.Required(category, nameof(category), 100);
        Frequency = frequency;
        PrimaryUsers = ReportValidation.Required(primaryUsers, nameof(primaryUsers), 500);
        RequiredPermission = ReportValidation.Required(requiredPermission, nameof(requiredPermission), 200);
        IsActive = true;
        CreatedByUserId = ReportValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ReportValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public ReportFrequency Frequency { get; private set; }
    public string PrimaryUsers { get; private set; } = string.Empty;
    public string RequiredPermission { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<ReportOwnerMatrix> Owners => _owners.AsReadOnly();

    public static ReportDefinition Create(string code, string name, string category, ReportFrequency frequency, string primaryUsers, string requiredPermission, string createdByUserId, DateTime createdAtUtc)
    {
        return new ReportDefinition(code, name, category, frequency, primaryUsers, requiredPermission, createdByUserId, createdAtUtc);
    }

    public void AddOwner(string ownerRole, string responsibility, string createdByUserId, DateTime createdAtUtc)
    {
        _owners.Add(ReportOwnerMatrix.Create(Id, ownerRole, responsibility, createdByUserId, createdAtUtc));
    }
}

public sealed class ReportOwnerMatrix : AuditableAggregateRoot
{
    private ReportOwnerMatrix()
    {
    }

    private ReportOwnerMatrix(Guid reportDefinitionId, string ownerRole, string responsibility, string createdByUserId, DateTime createdAtUtc)
    {
        ReportDefinitionId = reportDefinitionId;
        OwnerRole = ReportValidation.Required(ownerRole, nameof(ownerRole), 100);
        Responsibility = ReportValidation.Required(responsibility, nameof(responsibility), 500);
        CreatedByUserId = ReportValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ReportValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid ReportDefinitionId { get; private set; }
    public string OwnerRole { get; private set; } = string.Empty;
    public string Responsibility { get; private set; } = string.Empty;
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static ReportOwnerMatrix Create(Guid reportDefinitionId, string ownerRole, string responsibility, string createdByUserId, DateTime createdAtUtc)
    {
        return new ReportOwnerMatrix(reportDefinitionId, ownerRole, responsibility, createdByUserId, createdAtUtc);
    }
}

public sealed class ReportSchedule : AuditableAggregateRoot
{
    private ReportSchedule()
    {
    }

    private ReportSchedule(Guid reportDefinitionId, ReportFrequency frequency, string cronExpression, DateTime nextRunAtUtc, string createdByUserId, DateTime createdAtUtc)
    {
        ReportDefinitionId = reportDefinitionId;
        Frequency = frequency;
        CronExpression = ReportValidation.Required(cronExpression, nameof(cronExpression), 100);
        NextRunAtUtc = ReportValidation.EnsureUtc(nextRunAtUtc, nameof(nextRunAtUtc));
        Status = ReportScheduleStatus.Active;
        CreatedByUserId = ReportValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ReportValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid ReportDefinitionId { get; private set; }
    public ReportFrequency Frequency { get; private set; }
    public string CronExpression { get; private set; } = string.Empty;
    public DateTime NextRunAtUtc { get; private set; }
    public ReportScheduleStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static ReportSchedule Create(Guid reportDefinitionId, ReportFrequency frequency, string cronExpression, DateTime nextRunAtUtc, string createdByUserId, DateTime createdAtUtc)
    {
        return new ReportSchedule(reportDefinitionId, frequency, cronExpression, nextRunAtUtc, createdByUserId, createdAtUtc);
    }
}

public sealed class ReportRun : AuditableAggregateRoot
{
    private readonly List<ReportParameter> _parameters = [];
    private readonly List<ReportOutput> _outputs = [];
    private readonly List<ReportApproval> _approvals = [];
    private readonly List<ReportDistribution> _distributions = [];

    private ReportRun()
    {
    }

    private ReportRun(Guid reportDefinitionId, string reportCode, BusinessDate businessDate, DateTime sourceDataTimestampUtc, string generatedByUserId, DateTime generatedAtUtc)
    {
        ReportDefinitionId = reportDefinitionId;
        ReportCode = ReportValidation.Required(reportCode, nameof(reportCode), 50).ToUpperInvariant();
        BusinessDate = businessDate;
        SourceDataTimestampUtc = ReportValidation.EnsureUtc(sourceDataTimestampUtc, nameof(sourceDataTimestampUtc));
        Status = ReportRunStatus.Generated;
        VersionNumber = 1;
        GeneratedByUserId = ReportValidation.Required(generatedByUserId, nameof(generatedByUserId), 200);
        GeneratedAtUtc = ReportValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        MarkCreated(GeneratedByUserId, GeneratedAtUtc);
    }

    public Guid ReportDefinitionId { get; private set; }
    public string ReportCode { get; private set; } = string.Empty;
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public DateTime SourceDataTimestampUtc { get; private set; }
    public ReportRunStatus Status { get; private set; }
    public int VersionNumber { get; private set; }
    public string GeneratedByUserId { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? PublishedByUserId { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public IReadOnlyCollection<ReportParameter> Parameters => _parameters.AsReadOnly();
    public IReadOnlyCollection<ReportOutput> Outputs => _outputs.AsReadOnly();
    public IReadOnlyCollection<ReportApproval> Approvals => _approvals.AsReadOnly();
    public IReadOnlyCollection<ReportDistribution> Distributions => _distributions.AsReadOnly();

    public static ReportRun Create(Guid reportDefinitionId, string reportCode, BusinessDate businessDate, DateTime sourceDataTimestampUtc, string generatedByUserId, DateTime generatedAtUtc)
    {
        return new ReportRun(reportDefinitionId, reportCode, businessDate, sourceDataTimestampUtc, generatedByUserId, generatedAtUtc);
    }

    public void AddParameter(string name, string value)
    {
        _parameters.Add(ReportParameter.Create(Id, name, value));
    }

    public void AddOutput(ReportOutput output)
    {
        _outputs.Add(output);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        if (Status != ReportRunStatus.Generated)
        {
            throw new InvalidOperationException("Only generated report runs can be approved.");
        }

        var actor = ReportValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, GeneratedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: report generator cannot approve the same report run.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = ReportValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        Status = ReportRunStatus.Approved;
        _approvals.Add(ReportApproval.Create(Id, ReportApprovalDecision.Approved, actor, ApprovedAtUtc.Value, comment));
    }

    public void Publish(string publishedByUserId, DateTime publishedAtUtc, string? comment)
    {
        if (Status != ReportRunStatus.Approved)
        {
            throw new InvalidOperationException("Report run must be approved before official publication.");
        }

        PublishedByUserId = ReportValidation.Required(publishedByUserId, nameof(publishedByUserId), 200);
        PublishedAtUtc = ReportValidation.EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        Status = ReportRunStatus.Published;
        _distributions.Add(ReportDistribution.Create(Id, ReportDeliveryChannel.SecurePortal, "Official report published to secure portal.", PublishedByUserId, PublishedAtUtc.Value, comment));
    }
}

public sealed class ReportParameter : Entity
{
    private ReportParameter()
    {
    }

    private ReportParameter(Guid reportRunId, string name, string value)
    {
        ReportRunId = reportRunId;
        Name = ReportValidation.Required(name, nameof(name), 100);
        Value = ReportValidation.Required(value, nameof(value), 1000);
    }

    public Guid ReportRunId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    public static ReportParameter Create(Guid reportRunId, string name, string value)
    {
        return new ReportParameter(reportRunId, name, value);
    }
}

public sealed class ReportOutput : Entity
{
    private ReportOutput()
    {
    }

    private ReportOutput(Guid reportRunId, ReportOutputFormat format, string storageReference, string contentHash, long sizeBytes, DateTime createdAtUtc)
    {
        ReportRunId = reportRunId;
        Format = format;
        StorageReference = ReportValidation.Required(storageReference, nameof(storageReference), 500);
        ContentHash = ReportValidation.Required(contentHash, nameof(contentHash), 128);
        SizeBytes = ReportValidation.NonNegative(sizeBytes, nameof(sizeBytes));
        CreatedAtUtc = ReportValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid ReportRunId { get; private set; }
    public ReportOutputFormat Format { get; private set; }
    public string StorageReference { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ReportOutput Create(Guid reportRunId, ReportOutputFormat format, string storageReference, string contentHash, long sizeBytes, DateTime createdAtUtc)
    {
        return new ReportOutput(reportRunId, format, storageReference, contentHash, sizeBytes, createdAtUtc);
    }
}

public sealed class ReportApproval : Entity
{
    private ReportApproval()
    {
    }

    private ReportApproval(Guid reportRunId, ReportApprovalDecision decision, string decidedByUserId, DateTime decidedAtUtc, string? comment)
    {
        ReportRunId = reportRunId;
        Decision = decision;
        DecidedByUserId = ReportValidation.Required(decidedByUserId, nameof(decidedByUserId), 200);
        DecidedAtUtc = ReportValidation.EnsureUtc(decidedAtUtc, nameof(decidedAtUtc));
        Comment = ReportValidation.Optional(comment, 1000);
    }

    public Guid ReportRunId { get; private set; }
    public ReportApprovalDecision Decision { get; private set; }
    public string DecidedByUserId { get; private set; } = string.Empty;
    public DateTime DecidedAtUtc { get; private set; }
    public string? Comment { get; private set; }

    public static ReportApproval Create(Guid reportRunId, ReportApprovalDecision decision, string decidedByUserId, DateTime decidedAtUtc, string? comment)
    {
        return new ReportApproval(reportRunId, decision, decidedByUserId, decidedAtUtc, comment);
    }
}

public sealed class ReportDistribution : Entity
{
    private ReportDistribution()
    {
    }

    private ReportDistribution(Guid reportRunId, ReportDeliveryChannel channel, string recipient, string distributedByUserId, DateTime distributedAtUtc, string? comment)
    {
        ReportRunId = reportRunId;
        Channel = channel;
        Recipient = ReportValidation.Required(recipient, nameof(recipient), 300);
        DistributedByUserId = ReportValidation.Required(distributedByUserId, nameof(distributedByUserId), 200);
        DistributedAtUtc = ReportValidation.EnsureUtc(distributedAtUtc, nameof(distributedAtUtc));
        Comment = ReportValidation.Optional(comment, 1000);
    }

    public Guid ReportRunId { get; private set; }
    public ReportDeliveryChannel Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string DistributedByUserId { get; private set; } = string.Empty;
    public DateTime DistributedAtUtc { get; private set; }
    public string? Comment { get; private set; }

    public static ReportDistribution Create(Guid reportRunId, ReportDeliveryChannel channel, string recipient, string distributedByUserId, DateTime distributedAtUtc, string? comment)
    {
        return new ReportDistribution(reportRunId, channel, recipient, distributedByUserId, distributedAtUtc, comment);
    }
}

public sealed class ReportVersionArchive : AuditableAggregateRoot
{
    private ReportVersionArchive()
    {
    }

    private ReportVersionArchive(Guid reportRunId, int versionNumber, string archivePayloadJson, string payloadHash, string archivedByUserId, DateTime archivedAtUtc)
    {
        ReportRunId = reportRunId;
        VersionNumber = ReportValidation.Positive(versionNumber, nameof(versionNumber));
        ArchivePayloadJson = ReportValidation.Required(archivePayloadJson, nameof(archivePayloadJson), 20000);
        PayloadHash = ReportValidation.Required(payloadHash, nameof(payloadHash), 128);
        ArchivedByUserId = ReportValidation.Required(archivedByUserId, nameof(archivedByUserId), 200);
        ArchivedAtUtc = ReportValidation.EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
        MarkCreated(ArchivedByUserId, ArchivedAtUtc);
    }

    public Guid ReportRunId { get; private set; }
    public int VersionNumber { get; private set; }
    public string ArchivePayloadJson { get; private set; } = "{}";
    public string PayloadHash { get; private set; } = string.Empty;
    public string ArchivedByUserId { get; private set; } = string.Empty;
    public DateTime ArchivedAtUtc { get; private set; }

    public static ReportVersionArchive Create(Guid reportRunId, int versionNumber, string archivePayloadJson, string payloadHash, string archivedByUserId, DateTime archivedAtUtc)
    {
        return new ReportVersionArchive(reportRunId, versionNumber, archivePayloadJson, payloadHash, archivedByUserId, archivedAtUtc);
    }
}

public sealed class ReportBundle : AuditableAggregateRoot
{
    private ReportBundle()
    {
    }

    private ReportBundle(string bundleCode, string name, BusinessDate businessDate, string reportRunIdsJson, string createdByUserId, DateTime createdAtUtc)
    {
        BundleCode = ReportValidation.Required(bundleCode, nameof(bundleCode), 50).ToUpperInvariant();
        Name = ReportValidation.Required(name, nameof(name), 200);
        BusinessDate = businessDate;
        ReportRunIdsJson = ReportValidation.Required(reportRunIdsJson, nameof(reportRunIdsJson), 12000);
        Status = ReportBundleStatus.Draft;
        CreatedByUserId = ReportValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = ReportValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public string BundleCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string ReportRunIdsJson { get; private set; } = "[]";
    public ReportBundleStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? PublishedByUserId { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public static ReportBundle Create(string bundleCode, string name, BusinessDate businessDate, string reportRunIdsJson, string createdByUserId, DateTime createdAtUtc)
    {
        return new ReportBundle(bundleCode, name, businessDate, reportRunIdsJson, createdByUserId, createdAtUtc);
    }

    public void Publish(string publishedByUserId, DateTime publishedAtUtc)
    {
        PublishedByUserId = ReportValidation.Required(publishedByUserId, nameof(publishedByUserId), 200);
        PublishedAtUtc = ReportValidation.EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        Status = ReportBundleStatus.Published;
    }
}
