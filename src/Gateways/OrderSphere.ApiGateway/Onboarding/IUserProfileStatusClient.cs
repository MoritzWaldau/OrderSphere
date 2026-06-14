namespace OrderSphere.ApiGateway.Onboarding;

public interface IUserProfileStatusClient
{
    Task<bool> GetOnboardingStatusAsync(string bearerToken, CancellationToken ct = default);
}
