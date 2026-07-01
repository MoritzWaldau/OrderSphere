using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.ServiceDefaults;
using OrderSphere.UserProfile.Application.Features.Profile.AddAddress;
using OrderSphere.UserProfile.Application.Features.Profile.CompleteOnboarding;
using OrderSphere.UserProfile.Application.Features.Profile.DeleteAddress;
using OrderSphere.UserProfile.Application.Features.Profile.EnsureProfile;
using OrderSphere.UserProfile.Application.Features.Profile.GetAddresses;
using OrderSphere.UserProfile.Application.Features.Profile.GetNotificationPreferences;
using OrderSphere.UserProfile.Application.Features.Profile.OnboardingStatus;
using OrderSphere.UserProfile.Application.Features.Profile.RequestErasure;
using OrderSphere.UserProfile.Application.Features.Profile.SetDefaultAddress;
using OrderSphere.UserProfile.Application.Features.Profile.SkipOnboarding;
using OrderSphere.UserProfile.Application.Features.Profile.UpdateAddress;
using OrderSphere.UserProfile.Application.Features.Profile.UpdateNotificationPreferences;
using OrderSphere.UserProfile.Application.Features.Profile.UpdatePreferences;
using OrderSphere.UserProfile.Application.Features.Profile.UpdateProfile;
using OrderSphere.UserProfile.Application.Models;

namespace OrderSphere.UserProfile.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this RouteGroupBuilder v1)
    {
        var group = v1.MapGroup("profile").RequireAuthorization();

        group.MapGet("/", GetOrCreateProfile);
        group.MapPut("/", UpdateProfile);
        group.MapPut("/preferences", UpdatePreferences);
        group.MapGet("/notifications", GetNotificationPreferences);
        group.MapPut("/notifications", UpdateNotificationPreferences);
        group.MapGet("/onboarding-status", GetOnboardingStatus);
        group.MapPost("/complete-onboarding", CompleteOnboarding);
        group.MapPost("/skip-onboarding", SkipOnboarding);
        group.MapGet("/addresses", GetAddresses);
        group.MapPost("/addresses", AddAddress);
        group.MapPut("/addresses/{addressId:guid}", UpdateAddress);
        group.MapDelete("/addresses/{addressId:guid}", DeleteAddress);
        group.MapPost("/addresses/{addressId:guid}/set-default", SetDefaultAddress);
        group.MapPost("/erasure-request", RequestErasure);
    }

    private static async Task<IResult> GetOrCreateProfile(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(
            new EnsureProfileCommand(sub, currentUser.Name ?? string.Empty, currentUser.Email ?? string.Empty), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var email = currentUser.Email ?? string.Empty;
        var result = await sender.Send(new UpdateProfileCommand(sub, request.DisplayName, email), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new UpdatePreferencesCommand(sub, request.DarkModeEnabled), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetNotificationPreferences(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new GetNotificationPreferencesQuery(sub), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(
            new UpdateNotificationPreferencesCommand(sub, request.EmailEnabled, request.SmsEnabled, request.PushEnabled), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetOnboardingStatus(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new GetOnboardingStatusQuery(sub), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CompleteOnboarding(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new CompleteOnboardingCommand(sub), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SkipOnboarding(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(
            new SkipOnboardingCommand(sub, currentUser.Name ?? string.Empty, currentUser.Email ?? string.Empty), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAddresses(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new GetAddressesQuery(sub), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AddAddress(
        [FromBody] CreateAddressRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new AddAddressCommand(
            sub, request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country,
            request.SetAsDefault), ct);

        return result.ToHttpResult(
            address => Results.Created($"/api/v1/profile/addresses/{address.Id}", address));
    }

    private static async Task<IResult> UpdateAddress(
        Guid addressId,
        [FromBody] UpdateAddressRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new UpdateAddressCommand(
            sub, addressId,
            request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country), ct);

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteAddress(
        Guid addressId,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new DeleteAddressCommand(sub, addressId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SetDefaultAddress(
        Guid addressId,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new SetDefaultAddressCommand(sub, addressId), ct);
        return result.ToHttpResult();
    }

    // D1 — GDPR right-to-erasure. Self-service only: a customer erases their own data,
    // triggering anonymization here and a fan-out event to every other PII-holding service.
    private static async Task<IResult> RequestErasure(
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (currentUser.Sub is not { } sub) return Results.Unauthorized();

        var result = await sender.Send(new RequestErasureCommand(sub), ct);
        return result.ToHttpResult();
    }
}
