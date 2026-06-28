using MediatR;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Application.Features.Returns.Admin.GetAllReturns;
using OrderSphere.Ordering.Application.Features.Returns.ApproveReturn;
using OrderSphere.Ordering.Application.Features.Returns.GetReturnsByCustomer;
using OrderSphere.Ordering.Application.Features.Returns.RejectReturn;
using OrderSphere.Ordering.Application.Features.Returns.RequestReturn;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class ReturnEndpoints
{
    public static void MapReturnEndpoints(this RouteGroupBuilder v1)
    {
        // Customer-facing: create and list one's own return requests.
        var customer = v1.MapGroup("returns").RequireAuthorization();

        customer.MapPost("/",
            async (CreateReturnRequest body, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
            {
                if (!TryGetCustomerId(currentUser, out var customerId))
                    return Results.Unauthorized();

                var command = new RequestReturnCommand(
                    body.OrderId,
                    customerId,
                    body.Reason,
                    body.Items.Select(i => new RequestReturnLine(i.ProductId, i.Quantity)).ToList());

                var result = await mediator.Send(command, ct);
                return result.ToHttpResult(id => Results.Created($"/api/v1/returns/{id}", new { Id = id }));
            });

        customer.MapGet("/",
            async (ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
            {
                if (!TryGetCustomerId(currentUser, out var customerId))
                    return Results.Unauthorized();

                var result = await mediator.Send(new GetReturnsByCustomerQuery(customerId), ct);
                return result.ToHttpResult();
            });

        // Staff read: any staff role.
        var staffRead = v1.MapGroup("admin/returns").RequireAuthorization(AuthorizationPolicies.Staff);

        staffRead.MapGet("/",
            async (ReturnStatus? status, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAllReturnsQuery(status), ct);
                return result.ToHttpResult();
            });

        // Staff write: order-manager or admin may approve/reject.
        var orderManager = v1.MapGroup("admin/returns").RequireAuthorization(AuthorizationPolicies.OrderManager);

        orderManager.MapPost("/{id:guid}/approve",
            async (Guid id, ResolveReturnRequest? body, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new ApproveReturnCommand(id, body?.Note), ct);
                return result.ToHttpResult(() => Results.Ok());
            });

        orderManager.MapPost("/{id:guid}/reject",
            async (Guid id, ResolveReturnRequest? body, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new RejectReturnCommand(id, body?.Note), ct);
                return result.ToHttpResult(() => Results.Ok());
            });
    }

    private static bool TryGetCustomerId(ICurrentUser currentUser, out Guid customerId)
    {
        if (!currentUser.IsAuthenticated || currentUser.Sub is null)
        {
            customerId = Guid.Empty;
            return false;
        }
        customerId = CustomerId.FromSub(currentUser.Sub).Value;
        return true;
    }

    public sealed record CreateReturnRequest(Guid OrderId, string Reason, IReadOnlyList<CreateReturnLine> Items);

    public sealed record CreateReturnLine(Guid ProductId, int Quantity);

    public sealed record ResolveReturnRequest(string? Note);
}
