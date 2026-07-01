namespace OrderSphere.BuildingBlocks.Security;

/// <summary>
/// No-op <see cref="ICurrentUser"/> used exclusively by EF Core design-time factories
/// (<c>IDesignTimeDbContextFactory</c>) where a real DI container is unavailable.
/// Must not be registered in production DI.
/// </summary>
public sealed class NullCurrentUser : ICurrentUser
{
    public static readonly NullCurrentUser Instance = new();

    private NullCurrentUser() { }

    public string? Sub => null;
    public string? Name => null;
    public string? Email => null;
    public IReadOnlyList<string> Roles => [];
    public bool IsAuthenticated => false;
    public bool IsInRole(params string[] roles) => false;
}
