using Cis.Domain.Common;
using FluentAssertions;

namespace Cis.Tests.Unit.Common;

public sealed class BusinessDateTests
{
    [Fact]
    public void From_DateOnly_StoresDateWithoutTime()
    {
        var businessDate = BusinessDate.From(new DateOnly(2026, 5, 11));

        businessDate.Value.Should().Be(new DateOnly(2026, 5, 11));
        businessDate.ToString().Should().Be("2026-05-11");
    }

    [Fact]
    public void From_DateTime_UsesCalendarDate()
    {
        var businessDate = BusinessDate.From(new DateTime(2026, 5, 11, 23, 59, 59, DateTimeKind.Utc));

        businessDate.Value.Should().Be(new DateOnly(2026, 5, 11));
    }
}
