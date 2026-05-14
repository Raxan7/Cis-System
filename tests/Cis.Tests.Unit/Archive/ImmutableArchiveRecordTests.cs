using Cis.Domain.Archive;
using FluentAssertions;

namespace Cis.Tests.Unit.Archive;

public sealed class ImmutableArchiveRecordTests
{
    [Fact]
    public void Create_WhenPayloadIsSame_GeneratesSameHash()
    {
        var timestamp = new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Utc);

        var first = ImmutableArchiveRecord.Create(
            "Archive",
            "RegulatorPack",
            "pack-001",
            "{\"pack\":\"q2\"}",
            "auditor-001",
            timestamp,
            null,
            null,
            "Issued.");
        var second = ImmutableArchiveRecord.Create(
            "Archive",
            "RegulatorPack",
            "pack-002",
            "{\"pack\":\"q2\"}",
            "auditor-001",
            timestamp,
            null,
            null,
            "Issued.");

        first.PayloadHash.Should().Be(second.PayloadHash);
        first.ArchivedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_WhenTimestampIsNotUtc_Throws()
    {
        var action = () => ImmutableArchiveRecord.Create(
            "Archive",
            "RegulatorPack",
            "pack-001",
            "{}",
            "auditor-001",
            new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Local),
            null,
            null,
            "Issued.");

        action.Should().Throw<ArgumentException>();
    }
}
