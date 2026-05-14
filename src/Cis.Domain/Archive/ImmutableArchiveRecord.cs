using System.Security.Cryptography;
using System.Text;
using Cis.Domain.Common;

namespace Cis.Domain.Archive;

public sealed class ImmutableArchiveRecord : AggregateRoot
{
    private ImmutableArchiveRecord()
    {
    }

    private ImmutableArchiveRecord(
        string module,
        string entityType,
        string entityId,
        string payloadJson,
        string archivedByUserId,
        DateTime archivedAtUtc,
        Guid? workflowId,
        Guid? retentionPolicyId,
        string reason)
    {
        EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
        Module = Required(module, nameof(module), 100);
        EntityType = Required(entityType, nameof(entityType), 200);
        EntityId = Required(entityId, nameof(entityId), 100);
        PayloadJson = Required(payloadJson, nameof(payloadJson), 1_000_000);
        PayloadHash = Sha256(PayloadJson);
        ArchivedByUserId = Required(archivedByUserId, nameof(archivedByUserId), 200);
        ArchivedAtUtc = archivedAtUtc;
        WorkflowId = workflowId;
        RetentionPolicyId = retentionPolicyId;
        Reason = Required(reason, nameof(reason), 1000);
    }

    public string Module { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public string PayloadHash { get; private set; } = string.Empty;

    public string ArchivedByUserId { get; private set; } = string.Empty;

    public DateTime ArchivedAtUtc { get; private set; }

    public Guid? WorkflowId { get; private set; }

    public Guid? RetentionPolicyId { get; private set; }

    public RetentionPolicy? RetentionPolicy { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public static ImmutableArchiveRecord Create(
        string module,
        string entityType,
        string entityId,
        string payloadJson,
        string archivedByUserId,
        DateTime archivedAtUtc,
        Guid? workflowId,
        Guid? retentionPolicyId,
        string reason)
    {
        return new ImmutableArchiveRecord(module, entityType, entityId, payloadJson, archivedByUserId, archivedAtUtc, workflowId, retentionPolicyId, reason);
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
