namespace OrderSphere.UI.Models.Auth;

public sealed class LoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; } = false;
}
