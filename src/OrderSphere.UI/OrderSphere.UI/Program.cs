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
using OrderSphere.Infrastructure.Email;
using OrderSphere.Infrastructure.Persistence;
using OrderSphere.UI;
using OrderSphere.UI.Components;
using OrderSphere.UI.Configuration;
using OrderSphere.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddLogging()
    .AddServiceBus();

// Add services to the container.
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddMudServices();

builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler("/Error", createScopeForErrors: true);
app.UseHsts();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

await DataSeeder.SeedDataAsync(app);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapAuthEndpoints();

app.Run();
