using Cis.Domain.Audit;
using FluentAssertions;

namespace Cis.Tests.Unit.Audit;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_WhenTimestampIsUtc_CreatesImmutableAuditRecord()
    {
        var occurredAtUtc = new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Utc);

        var auditLog = AuditLog.Create(
            "Schemes",
            "Create",
            "Scheme",
            "scheme-001",
            "maker-001",
            "Fund Maker",
            occurredAtUtc,
            summary: "Created scheme draft.");

        auditLog.Module.Should().Be("Schemes");
        auditLog.Action.Should().Be("Create");
        auditLog.OccurredAtUtc.Should().Be(occurredAtUtc);
        auditLog.Summary.Should().Be("Created scheme draft.");
    }

    [Fact]
    public void Create_WhenTimestampIsNotUtc_Throws()
    {
        var action = () => AuditLog.Create(
            "Schemes",
            "Create",
            "Scheme",
            "scheme-001",
            "maker-001",
            "Fund Maker",
            new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Local));

        action.Should().Throw<ArgumentException>();
    }
}
