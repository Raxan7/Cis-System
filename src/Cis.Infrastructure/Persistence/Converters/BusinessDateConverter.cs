using Cis.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cis.Infrastructure.Persistence.Converters;

internal sealed class BusinessDateConverter : ValueConverter<BusinessDate, DateOnly>
{
    public BusinessDateConverter()
        : base(value => value.Value, value => BusinessDate.From(value))
    {
    }
}
