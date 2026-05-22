using Microsoft.EntityFrameworkCore;
using OrderSphere.UserProfile.Api.Models;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.UserProfile.Api.Endpoints;

public static class AdminProfileEndpoints
{
    public static void MapAdminProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin/users").RequireAuthorization("AdminPolicy");

        admin.MapGet("/", GetAllUsers);
        admin.MapGet("/{id:guid}", GetUserById);
    }

    private static async Task<IResult> GetAllUsers(
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profiles = await context.CustomerProfiles
            .AsNoTracking()
            .Include(p => p.Addresses)
            .OrderBy(p => p.DisplayName)
            .ToListAsync(ct);

        var dtos = profiles.Select(p => new AdminUserSummaryDto(
            p.Id,
            p.KeycloakSubject,
            p.DisplayName,
            p.Email,
            p.DarkModeEnabled,
            p.Addresses.Count)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetUserById(
        Guid id,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .AsNoTracking()
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (profile is null) return Results.NotFound();

        var dto = new ProfileDto(
            profile.Id,
            profile.KeycloakSubject,
            profile.DisplayName,
            profile.Email,
            profile.DarkModeEnabled,
            profile.Addresses.Select(a => new AddressDto(
                a.Id, a.Label, a.FirstName, a.LastName,
                a.Street, a.City, a.PostalCode, a.Country, a.IsDefault)).ToList());

        return Results.Ok(dto);
    }
}
