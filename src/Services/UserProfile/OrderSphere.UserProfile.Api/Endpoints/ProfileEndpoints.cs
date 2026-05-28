using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.UserProfile.Api.Features.Profile.AddAddress;
using OrderSphere.UserProfile.Api.Features.Profile.DeleteAddress;
using OrderSphere.UserProfile.Api.Features.Profile.GetAddresses;
using OrderSphere.UserProfile.Api.Features.Profile.GetOrCreateProfile;
using OrderSphere.UserProfile.Api.Features.Profile.SetDefaultAddress;
using OrderSphere.UserProfile.Api.Features.Profile.UpdateAddress;
using OrderSphere.UserProfile.Api.Features.Profile.UpdatePreferences;
using OrderSphere.UserProfile.Api.Features.Profile.UpdateProfile;
using OrderSphere.UserProfile.Api.Models;
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
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var displayName = user.FindFirstValue("name") ?? user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? string.Empty;

        var result = await sender.Send(new GetOrCreateProfileQuery(sub, displayName, email), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem();
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new UpdateProfileCommand(sub, request.DisplayName, request.Email), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
    }

    private static async Task<IResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new UpdatePreferencesCommand(sub, request.DarkModeEnabled), ct);
        return result.IsSuccess ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetAddresses(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new GetAddressesQuery(sub), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
    }

    private static async Task<IResult> AddAddress(
        [FromBody] CreateAddressRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new AddAddressCommand(
            sub, request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country,
            request.SetAsDefault), ct);

        if (!result.IsSuccess)
        {
            return result.Error.Code == "UserProfile.Address.LimitExceeded"
                ? Results.BadRequest(new { error = result.Error.Description })
                : Results.NotFound();
        }

        return Results.Created($"/api/v1/profile/addresses/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> UpdateAddress(
        Guid addressId,
        [FromBody] UpdateAddressRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new UpdateAddressCommand(
            sub, addressId,
            request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country), ct);

        return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
    }

    private static async Task<IResult> DeleteAddress(
        Guid addressId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new DeleteAddressCommand(sub, addressId), ct);
        return result.IsSuccess ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> SetDefaultAddress(
        Guid addressId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var sub = GetSubject(user);
        if (sub is null) return Results.Unauthorized();

        var result = await sender.Send(new SetDefaultAddressCommand(sub, addressId), ct);
        return result.IsSuccess ? Results.NoContent() : Results.NotFound();
    }

    private static string? GetSubject(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
}
