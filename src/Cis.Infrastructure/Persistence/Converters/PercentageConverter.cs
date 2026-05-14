using Cis.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cis.Infrastructure.Persistence.Converters;

internal sealed class PercentageConverter : ValueConverter<Percentage, decimal>
{
    public PercentageConverter()
        : base(value => value.Value, value => Percentage.FromPercent(value))
    {
    }
}
