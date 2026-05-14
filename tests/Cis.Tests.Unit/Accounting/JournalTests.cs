using Cis.Domain.Accounting;
using Cis.Domain.Common;
using FluentAssertions;

namespace Cis.Tests.Unit.Accounting;

public sealed class JournalTests
{
    [Fact]
    public void EnsureBalanced_WithEqualDebitsAndCredits_DoesNotThrow()
    {
        var journal = CreateJournal();
        journal.AddLine(Guid.NewGuid(), "Debit cash", 100m, 0m);
        journal.AddLine(Guid.NewGuid(), "Credit units", 0m, 100m);

        var act = () => journal.EnsureBalanced();

        act.Should().NotThrow();
        journal.TotalDebits.Should().Be(100m);
        journal.TotalCredits.Should().Be(100m);
    }

    [Fact]
    public void EnsureBalanced_WithUnbalancedLines_Throws()
    {
        var journal = CreateJournal();
        journal.AddLine(Guid.NewGuid(), "Debit cash", 100m, 0m);
        journal.AddLine(Guid.NewGuid(), "Credit units", 0m, 90m);

        var act = () => journal.EnsureBalanced();

        act.Should().Throw<InvalidOperationException>().WithMessage("*balance*");
    }

    [Fact]
    public void AddLine_WithDebitAndCreditOnSameLine_Throws()
    {
        var journal = CreateJournal();

        var act = () => journal.AddLine(Guid.NewGuid(), "Invalid line", 100m, 100m);

        act.Should().Throw<ArgumentException>().WithMessage("*either a debit or a credit*");
    }

    [Fact]
    public void ApproveAndPost_PreventsSubmitterApprovingOwnJournal()
    {
        var now = DateTime.UtcNow;
        var journal = CreateJournal();
        journal.AddLine(Guid.NewGuid(), "Debit cash", 100m, 0m);
        journal.AddLine(Guid.NewGuid(), "Credit units", 0m, 100m);
        journal.Submit("maker", now, "Submit.");

        var act = () => journal.ApproveAndPost("maker", now.AddMinutes(1), "Approve.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Segregation of duties*");
    }

    private static Journal CreateJournal()
    {
        return Journal.CreateManual(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessDate.From(new DateOnly(2026, 5, 12)),
            "KES",
            "Manual journal",
            true,
            false,
            null,
            "maker",
            DateTime.UtcNow);
    }
}
