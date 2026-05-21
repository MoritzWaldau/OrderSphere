namespace OrderSphere.Domain.Primitives;

public sealed record Error(
    string Code,
    string Description
)
{
    public static readonly Error None = new("", "");

    public static readonly Error NullValue = new("General.Null", "Null value was provided.");
}
