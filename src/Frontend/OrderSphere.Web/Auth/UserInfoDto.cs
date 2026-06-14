namespace OrderSphere.Web.Auth;

public sealed record UserInfoDto(
    bool IsAuthenticated,
    string? Sub,
    string? Name,
    string? Email,
    string[]? Roles,
    bool OnboardingComplete,
    string? XsrfToken);
