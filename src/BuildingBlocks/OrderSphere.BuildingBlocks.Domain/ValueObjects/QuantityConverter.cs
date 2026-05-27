using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OrderSphere.BuildingBlocks.ValueObjects;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/> that maps
/// <see cref="Quantity"/> to and from a plain <see langword="int"/> column.
/// Register globally via
/// <c>configurationBuilder.Properties&lt;Quantity&gt;().HaveConversion&lt;QuantityConverter&gt;()</c>.
/// </summary>
public sealed class QuantityConverter : ValueConverter<Quantity, int>
{
    public QuantityConverter()
        : base(q => q.Value, v => new Quantity(v)) { }
}
