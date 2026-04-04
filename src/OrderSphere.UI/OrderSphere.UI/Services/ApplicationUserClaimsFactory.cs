using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OrderSphere.Domain.Entities;
using System.Security.Claims;

namespace OrderSphere.UI.Services;

public sealed class ApplicationUserClaimsFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("FirstName", user.FirstName));
        identity.AddClaim(new Claim("LastName", user.LastName));
        return identity;
    }
}
