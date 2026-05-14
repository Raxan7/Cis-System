using Cis.Domain.Common;
using FluentAssertions;

namespace Cis.Tests.Unit.Common;

public sealed class MoneyTests
{
    [Fact]
    public void Add_WhenCurrenciesMatch_ReturnsSum()
    {
        var left = Money.From(100.25m, "KES");
        var right = Money.From(50.75m, "KES");

        var result = left.Add(right);

        result.Amount.Should().Be(151.00m);
        result.Currency.Should().Be("KES");
    }

    [Fact]
    public void Add_WhenCurrenciesDiffer_Throws()
    {
        var left = Money.From(100m, "KES");
        var right = Money.From(10m, "USD");

        var action = () => left.Add(right);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void From_WhenCurrencyIsInvalid_Throws()
    {
        var action = () => Money.From(10m, "KE");

        action.Should().Throw<ArgumentException>();
    }
}
