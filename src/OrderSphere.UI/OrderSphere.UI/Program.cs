using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OrderSphere.Application;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Features.Checkout;
using OrderSphere.Application.Models;
using OrderSphere.Application.ServiceBus;
using OrderSphere.Domain.Entities;
using OrderSphere.Infrastructure;
using OrderSphere.Infrastructure.Persistence;
using OrderSphere.UI;
using OrderSphere.UI.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddLogging()
    .AddServiceBus();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services
    .AddServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddMudServices();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();


using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<OrderSphereDbContext>();
dbContext.Database.EnsureCreated();


app.MapGet("/Test", async ([FromServices] IDbContext context) =>
{
    try
    {
        await context.BeginTransactionAsync();
        var customerId = Guid.NewGuid();
        Product product = new("iPhone 16 Pro", "Apple iPhone 16 Pro - Neustes Modell", 1199.99m, 5);

        await context.Products.AddAsync(product);

        CartItem cartItem = new(product.Id, 3);
        Cart cart = new(customerId);
        cart.AddItem(cartItem);

        await context.Carts.AddAsync(cart);
        await context.CommitAsync();

        return Results.Ok("Cart created successful");
    }
    catch (Exception ex)
    {
        await context.RollbackAsync();
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/GetCart", async ([FromServices] IDbContext context) =>
{
    var carts = await context.Carts.Include(x => x.Items).ToListAsync();

    if(carts != null)
    {
        return Results.Ok(carts);
    }

    return Results.BadRequest();
});

app.MapGet("/GetProducts", async ([FromServices] IDbContext context) =>
{
    var products = await context.Products.ToListAsync();
    if(products != null)
    {
        return Results.Ok(products);
    }
    return Results.BadRequest();
});

app.MapGet("/CreateProduct", async ([FromServices] IDbContext context) =>
{
    await context.BeginTransactionAsync();

    for (int i = 1; i < 10; i++)
    {
        Product product = new($"Product {i}", $"Description for Product {i}", 9.99m + i, 10 + i);
        await context.Products.AddAsync(product);
    }

    await context.CommitAsync();

    return TypedResults.Ok("Products created successfully");
});

app.MapPost("/CheckoutCart", async ([FromServices] ISender sender, CheckoutCartDto checkouDto) =>
{
    var result = await sender.Send(new CheckoutCartCommand(checkouDto));
    return result;
});


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OrderSphere.UI.Client._Imports).Assembly);

app.Run();
