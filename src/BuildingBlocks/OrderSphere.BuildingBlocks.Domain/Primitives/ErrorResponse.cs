namespace OrderSphere.BuildingBlocks.Primitives;

/// <summary>
/// Uniform error body returned by all API endpoints on failure.
/// </summary>
public sealed record ErrorResponse(string Code, string Message);
