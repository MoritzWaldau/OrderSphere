using MediatR;
using OrderSphere.ServiceDefaults;
using OrderSphere.UserProfile.Application.Features.Profile.Admin.GetAllUsers;
using OrderSphere.UserProfile.Application.Features.Profile.Admin.GetUserById;

namespace OrderSphere.UserProfile.Api.Endpoints;

public static class AdminProfileEndpoints
{
    public static void MapAdminProfileEndpoints(this RouteGroupBuilder v1)
    {
        var admin = v1.MapGroup("admin/users").RequireAuthorization("AdminPolicy");

        admin.MapGet("/", GetAllUsers);
        admin.MapGet("/{id:guid}", GetUserById);
    }

    private static async Task<IResult> GetAllUsers(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAllUsersQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUserById(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), ct);
        return result.ToHttpResult();
    }
}
