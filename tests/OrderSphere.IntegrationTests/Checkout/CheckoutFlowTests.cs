using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Azure.Messaging.ServiceBus;

namespace OrderSphere.IntegrationTests.Checkout;

// Skeleton for the end-to-end checkout flow. Implementation is pending wiring on the UI side
// (a checkout endpoint or a test-only mediator entry point) plus DB seeding helpers.
[Collection(nameof(AspireCollection))]
public sealed class CheckoutFlowTests(AspireFixture fixture)
{
    [Fact(Skip = "Pending: seed cart in Postgres, POST /checkout to UI, poll Orders table, assert captured email.")]
    public async Task Checkout_PublishesEvent_WorkerCreatesOrder_EmailCaptured()
    {
        // 1. Seed Postgres with a Cart + Product via a scoped DbContext from fixture.App.Services.
        // 2. Invoke checkout (HTTP POST against ordersphere-ui or send CheckoutCartCommand via MediatR).
        // 3. Poll the orders Service Bus queue / Orders table until the Worker has consumed and persisted the order.
        // 4. Assert fixture.Email.OrderConfirmations contains the correct line items.

        var connectionString = await fixture.App.GetConnectionStringAsync("azure-service-bus");
        await using var client = new ServiceBusClient(connectionString);

        await Task.CompletedTask;
    }
}
