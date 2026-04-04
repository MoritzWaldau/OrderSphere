using Microsoft.AspNetCore.Identity;

namespace OrderSphere.Domain.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
