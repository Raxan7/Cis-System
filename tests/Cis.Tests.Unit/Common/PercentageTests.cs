using Cis.Domain.Common;
using FluentAssertions;

namespace Cis.Tests.Unit.Common;

public sealed class PercentageTests
{
    [Fact]
    public void FromPercent_StoresDecimalPercentValue()
    {
        var percentage = Percentage.FromPercent(12.345m);

        percentage.Value.Should().Be(12.345m);
        percentage.Ratio.Should().Be(0.12345m);
    }

    [Fact]
    public void FromRatio_ConvertsToPercent()
    {
        var percentage = Percentage.FromRatio(0.075m);

        percentage.Value.Should().Be(7.5m);
    }
}
