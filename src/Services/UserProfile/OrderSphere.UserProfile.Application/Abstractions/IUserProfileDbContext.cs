using Microsoft.EntityFrameworkCore;
using OrderSphere.UserProfile.Domain.Entities;

namespace OrderSphere.UserProfile.Application.Abstractions;

public interface IUserProfileDbContext
{
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<SavedAddress> SavedAddresses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
