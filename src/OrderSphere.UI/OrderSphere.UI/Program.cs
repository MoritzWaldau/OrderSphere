using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
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
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddLogging();

builder.AddOrderSphereCore();

builder.AddRedisClient("redis");
builder.AddRedisDistributedCache("redis");
builder.AddRedisOutputCache("redis");

builder.Services.AddOutputCache();


builder.Services.AddDataProtection()
    .SetApplicationName("OrderSphere");

builder.Services.AddOptions<KeyManagementOptions>()
    .Configure<IConnectionMultiplexer>((options, multiplexer) =>
    {
        options.XmlRepository = new RedisXmlRepository(
            () => multiplexer.GetDatabase(),
            "DataProtection-Keys");
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServices(builder.Configuration);

builder.Services.AddMudServices();

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IThemeService, ThemeService>();

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


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { },
    KnownProxies = { }
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

await DataSeeder.SeedDataAsync(app);


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapAuthEndpoints();

app.Run();
