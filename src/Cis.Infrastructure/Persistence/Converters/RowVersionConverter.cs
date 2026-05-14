using Cis.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cis.Infrastructure.Persistence.Converters;

internal sealed class RowVersionConverter : ValueConverter<RowVersion, Guid>
{
    public RowVersionConverter()
        : base(value => value.Value, value => RowVersion.From(value))
    {
    }
}
