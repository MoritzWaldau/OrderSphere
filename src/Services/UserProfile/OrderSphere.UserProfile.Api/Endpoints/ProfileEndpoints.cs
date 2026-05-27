using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.UserProfile.Api.Models;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Infrastructure.Persistence;
using System.Security.Claims;

namespace OrderSphere.UserProfile.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile").RequireAuthorization();

        group.MapGet("/", GetOrCreateProfile);
        group.MapPut("/", UpdateProfile);
        group.MapPut("/preferences", UpdatePreferences);
        group.MapGet("/addresses", GetAddresses);
        group.MapPost("/addresses", AddAddress);
        group.MapPut("/addresses/{addressId:guid}", UpdateAddress);
        group.MapDelete("/addresses/{addressId:guid}", DeleteAddress);
        group.MapPost("/addresses/{addressId:guid}/set-default", SetDefaultAddress);
    }

    private static async Task<IResult> GetOrCreateProfile(
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.KeycloakSubject == sub, ct);

        if (profile is null)
        {
            var displayName = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? string.Empty;

            profile = new CustomerProfile(sub, displayName, email);
            context.CustomerProfiles.Add(profile);
            await context.SaveChangesAsync(ct);
        }

        return Results.Ok(ToDto(profile));
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        profile.UpdateDetails(request.DisplayName, request.Email);
        await context.SaveChangesAsync(ct);
        return Results.Ok(ToDto(profile));
    }

    private static async Task<IResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        profile.SetDarkMode(request.DarkModeEnabled);
        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAddresses(
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        return Results.Ok(profile.Addresses.Select(ToAddressDto).ToList());
    }

    private static async Task<IResult> AddAddress(
        [FromBody] CreateAddressRequest request,
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        if (profile.Addresses.Count >= 10)
            return Results.BadRequest(new { error = "Maximum number of saved addresses (10) reached." });

        var address = profile.AddAddress(
            request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country,
            request.SetAsDefault);

        await context.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/profile/addresses/{address.Id.Value}", ToAddressDto(address));
    }

    private static async Task<IResult> UpdateAddress(
        Guid addressId,
        [FromBody] UpdateAddressRequest request,
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        var address = profile.Addresses.FirstOrDefault(a => a.Id == SavedAddressId.From(addressId));
        if (address is null) return Results.NotFound();

        address.Update(
            request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country);

        await context.SaveChangesAsync(ct);
        return Results.Ok(ToAddressDto(address));
    }

    private static async Task<IResult> DeleteAddress(
        Guid addressId,
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        var removed = profile.RemoveAddress(SavedAddressId.From(addressId));
        if (!removed) return Results.NotFound();

        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetDefaultAddress(
        Guid addressId,
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var profile = await GetProfile(user, context, ct);
        if (profile is null) return Results.NotFound();

        var success = profile.SetDefaultAddress(SavedAddressId.From(addressId));
        if (!success) return Results.NotFound();

        await context.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<CustomerProfile?> GetProfile(
        ClaimsPrincipal user,
        UserProfileDbContext context,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return null;

        return await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.KeycloakSubject == sub, ct);
    }

    private static string? GetSubject(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

    private static ProfileDto ToDto(CustomerProfile p) => new(
        p.Id.Value,
        p.KeycloakSubject,
        p.DisplayName,
        p.Email,
        p.DarkModeEnabled,
        p.Addresses.Select(ToAddressDto).ToList());

    private static AddressDto ToAddressDto(SavedAddress a) => new(
        a.Id.Value, a.Label, a.FirstName, a.LastName,
        a.Street, a.City, a.PostalCode, a.Country, a.IsDefault);
}
