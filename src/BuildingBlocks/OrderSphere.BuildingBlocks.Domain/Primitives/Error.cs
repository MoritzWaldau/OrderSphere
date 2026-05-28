namespace OrderSphere.BuildingBlocks.Primitives;

public sealed record Error(
    string Code,
    string Description
)
{
    public static readonly Error None = new("", "");

    public static readonly Error NullValue = new("General.Null", "Null value was provided.");

    /// <summary>
    /// Creates a validation failure error with a concatenated message from all failure descriptions.
    /// </summary>
    public static Error ValidationFailure(string message) =>
        new("Validation.Invalid", message);
}
