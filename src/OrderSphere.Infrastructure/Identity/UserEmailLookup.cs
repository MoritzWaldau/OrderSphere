using Microsoft.EntityFrameworkCore;
using OrderSphere.Application.Abstraction;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.Infrastructure.Identity;

public sealed class UserEmailLookup(OrderSphereDbContext dbContext) : IUserEmailLookup
{
    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var idString = userId.ToString();

        return await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == idString)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
