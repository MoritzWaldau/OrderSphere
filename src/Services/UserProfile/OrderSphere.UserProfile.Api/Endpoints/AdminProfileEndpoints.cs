using MediatR;
using OrderSphere.ServiceDefaults;
using OrderSphere.UserProfile.Application.Features.Profile.Admin.GetAllUsers;
using OrderSphere.UserProfile.Application.Features.Profile.Admin.GetUserById;

namespace OrderSphere.UserProfile.Api.Endpoints;

public static class AdminProfileEndpoints
{
    public static void MapAdminProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin/users").RequireAuthorization("AdminPolicy");

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
