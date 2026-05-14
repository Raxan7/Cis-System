using Cis.Domain.Common;
using Cis.Domain.UnitRegister;
using FluentAssertions;

namespace Cis.Tests.Unit.UnitRegister;

public sealed class UnitLedgerEntryTests
{
    [Fact]
    public void Create_WithUnapprovedSource_Throws()
    {
        var source = UnitMovementSource.Create(
            UnitMovementSourceType.Adjustment,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ADJ-UNAPPROVED",
            isApproved: false,
            "checker",
            DateTime.UtcNow);

        var action = () => UnitLedgerEntry.Create(
            UnitMovementType.Adjustment,
            source.InvestorId,
            source.SchemeId,
            source.SchemeClassId,
            BusinessDate.From(new DateOnly(2026, 5, 12)),
            10m,
            source,
            "ADJ-UNAPPROVED",
            6,
            "poster",
            DateTime.UtcNow);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*source must be approved*");
    }

    [Fact]
    public void Create_RoundsUnitsToConfiguredPrecision()
    {
        var source = UnitMovementSource.Create(
            UnitMovementSourceType.Adjustment,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ADJ-PRECISION",
            isApproved: true,
            "checker",
            DateTime.UtcNow);

        var entry = UnitLedgerEntry.Create(
            UnitMovementType.Adjustment,
            source.InvestorId,
            source.SchemeId,
            source.SchemeClassId,
            BusinessDate.From(new DateOnly(2026, 5, 12)),
            12.1234567m,
            source,
            "ADJ-PRECISION",
            6,
            "poster",
            DateTime.UtcNow);

        entry.Units.Should().Be(12.123457m);
    }
}
