namespace OrderSphere.Application.Abstraction;

public interface IUserEmailLookup
{
    Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default);
}
