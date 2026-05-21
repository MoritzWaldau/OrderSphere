namespace OrderSphere.UI.Models.Auth;

public sealed class CurrentUserInfo
{
    public bool IsAuthenticated { get; init; }
    public string? UserId { get; init; }
    public Guid? CustomerId => Guid.TryParse(UserId, out var customerId) ? customerId : null;
    public string? UserName { get; init; }
    public string? Email { get; init; }
}
