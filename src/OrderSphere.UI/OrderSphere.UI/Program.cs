using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Events;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.ValueObjects;
using OrderSphere.Hosting;
using OrderSphere.UI;
using OrderSphere.UI.Components;
using OrderSphere.UI.Configuration;
using OrderSphere.UI.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddLogging();

builder.AddOrderSphereCore();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServices(builder.Configuration);

builder.Services.AddMudServices();

builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.MapDefaultEndpoints();


app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

await DataSeeder.SeedDataAsync(app);


app.MapGet("/send", async (IServiceBusPublisher serviceBusPublisher) =>
{
    await serviceBusPublisher.PublishCheckoutCartEventAsync(new CheckoutCartEvent(
        Guid.CreateVersion7(),
        new CheckoutCartDto(Guid.CreateVersion7(), new Address("Moritz", "Waldau", "Schwarmstedter Str. 2", "Essel", "29690", "Germany"), PaymentMethod.Invoice),
        [])
    );
});

app.MapGet("/receive", async (ServiceBusClient serviceBusClient) =>
{
    await using var receiver = serviceBusClient.CreateReceiver("orders");

    var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10, maxWaitTime: TimeSpan.FromSeconds(3));

    var bodies = new List<string>();
    foreach (var message in messages)
    {
        bodies.Add(message.Body.ToString());
        await receiver.CompleteMessageAsync(message);
    }

    return Results.Ok(bodies);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapAuthEndpoints();

app.Run();
