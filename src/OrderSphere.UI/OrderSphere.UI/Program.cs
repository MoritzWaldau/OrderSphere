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


using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<OrderSphereDbContext>();
dbContext.Database.EnsureCreated();

await dbContext.BeginTransactionAsync();

// Create Categories
var computerCategory = new Category("Computer", "Laptops, Desktops und Computer");
var smartphoneCategory = new Category("Smartphones", "Mobile Geräte und Smartphones");
var tabletCategory = new Category("Tablets", "Tablet-Computer und iPad");
var accessoriesCategory = new Category("Accessories", "Zubehör und Erweiterungen");

await dbContext.Categories.AddAsync(computerCategory);
await dbContext.Categories.AddAsync(smartphoneCategory);
await dbContext.Categories.AddAsync(tabletCategory);
await dbContext.Categories.AddAsync(accessoriesCategory);

// Save categories first to get their IDs
await dbContext.SaveChangesAsync();

// Computer Products
var computerProducts = new List<Product>
{
    new("MacBook Pro 16\" M4 Max", "High-performance laptop with Apple M4 Max chip, 16GB RAM, 512GB SSD, Retina display", 2499.99m, 8, computerCategory.Id, "APPL-MBP16-M4-MAX"),
    new("MacBook Air 15\" M3", "Ultrabook with M3 chip, 8GB RAM, 256GB SSD, ideal for professionals", 1499.99m, 12, computerCategory.Id, "APPL-MBA15-M3"),
    new("Mac Mini M4", "Compact desktop computer with M4 chip, 16GB RAM, 512GB SSD, perfect for home office", 899.99m, 15, computerCategory.Id, "APPL-MINI-M4"),
    new("Samsung Galaxy Book4 Pro", "Windows Laptop with Intel i7, 16GB RAM, 512GB SSD, AMOLED display, 6-hour battery", 1799.99m, 10, computerCategory.Id, "SAMS-BOOK4-I7"),
    new("Samsung Galaxy Book4", "Windows Laptop with Intel i5, 8GB RAM, 256GB SSD, FHD display, lightweight design", 999.99m, 14, computerCategory.Id, "SAMS-BOOK4-I5"),
};

// Smartphone Products
var smartphoneProducts = new List<Product>
{
    new("iPhone 16 Pro Max", "6.9\" display, A18 Pro chip, Camera system with 48MP main camera, titanium design", 1299.99m, 20, smartphoneCategory.Id, "APPL-IP16-PROMAX"),
    new("iPhone 16 Pro", "6.3\" display, A18 Pro chip, Triple camera setup, titanium design, ProMotion display", 999.99m, 25, smartphoneCategory.Id, "APPL-IP16-PRO"),
    new("iPhone 16", "6.1\" display, A18 chip, dual camera system, all-day battery life", 799.99m, 30, smartphoneCategory.Id, "APPL-IP16"),
    new("iPhone 15", "6.1\" display, A17 Pro chip, dual camera, Dynamic Island, USB-C charging", 699.99m, 18, smartphoneCategory.Id, "APPL-IP15"),
    new("Samsung Galaxy S24 Ultra", "6.8\" Dynamic AMOLED display, Snapdragon 8 Gen 3, 200MP camera, IP68 waterproof", 1299.99m, 16, smartphoneCategory.Id, "SAMS-S24-ULTRA"),
    new("Samsung Galaxy S24", "6.2\" Dynamic AMOLED display, Snapdragon 8 Gen 3, AI features, all-day battery", 999.99m, 22, smartphoneCategory.Id, "SAMS-S24"),
    new("Samsung Galaxy A55", "6.6\" Super AMOLED display, Exynos 1480, 50MP camera, budget-friendly", 449.99m, 40, smartphoneCategory.Id, "SAMS-A55"),
    new("Samsung Galaxy A25", "6.5\" AMOLED display, Exynos 1280, 50MP camera, excellent value for money", 299.99m, 50, smartphoneCategory.Id, "SAMS-A25"),
};

// Tablet Products
var tabletProducts = new List<Product>
{
    new("iPad Pro 12.9\" M4", "12.9\" Liquid Retina XDR display, M4 chip, ProMotion 120Hz, Apple Pencil Pro support", 1499.99m, 12, tabletCategory.Id, "APPL-IPP129-M4"),
    new("iPad Pro 11\" M4", "11\" Liquid Retina XDR display, M4 chip, FaceID, ideal for creators", 999.99m, 15, tabletCategory.Id, "APPL-IPP11-M4"),
    new("iPad Air 11\"", "11\" M2 chip, FaceID, compact design, Apple Pencil compatibility", 799.99m, 18, tabletCategory.Id, "APPL-IPA11-M2"),
    new("iPad (10th Gen)", "10.9\" display, A14 Bionic, USB-C, affordable iPad option", 349.99m, 25, tabletCategory.Id, "APPL-IPD10-A14"),
    new("Samsung Galaxy Tab S9 Ultra", "14.6\" Super AMOLED 120Hz, Snapdragon 8 Gen 2, S Pen included, IP68", 1199.99m, 10, tabletCategory.Id, "SAMS-TAB-S9-ULTRA"),
    new("Samsung Galaxy Tab S9+", "12.4\" Super AMOLED, Snapdragon 8 Gen 2, S Pen, premium design", 849.99m, 14, tabletCategory.Id, "SAMS-TAB-S9-PLUS"),
    new("Samsung Galaxy Tab A9", "8.7\" IPS LCD, MediaTek Helio, budget tablet for entertainment", 249.99m, 30, tabletCategory.Id, "SAMS-TAB-A9"),
};

// Accessories Products
var accessoriesProducts = new List<Product>
{
    new("Apple AirPods Pro (2nd Gen)", "Active noise cancellation, spatial audio, up to 6 hours listening time per charge", 249.99m, 40, accessoriesCategory.Id, "APPL-ADP2"),
    new("Apple AirPods Max", "Over-ear headphones with spatial audio, up to 20 hours battery, premium design", 549.99m, 20, accessoriesCategory.Id, "APPL-ADPMAX"),
    new("Apple Magic Keyboard", "Wireless keyboard with rechargeable battery, scissor mechanism, sleek design", 99.99m, 35, accessoriesCategory.Id, "APPL-MKB"),
    new("Apple USB-C to Lightning Cable (2m)", "Fast charging and data transfer cable for iPhones and iPads", 29.99m, 100, accessoriesCategory.Id, "APPL-USBC-LTG"),
    new("Samsung Galaxy Buds3 Pro", "Active noise cancellation, 360-degree audio, IPX7 waterproof, touch controls", 229.99m, 45, accessoriesCategory.Id, "SAMS-BUDS3PRO"),
    new("Samsung Galaxy Watch6", "AMOLED display, health tracking, water resistant, Samsung ecosystem", 299.99m, 25, accessoriesCategory.Id, "SAMS-WATCH6"),
    new("Samsung S Pen", "Pressure-sensitive stylus for Galaxy Tab and Note devices, 4096 pressure levels", 79.99m, 50, accessoriesCategory.Id, "SAMS-SPEN"),
    new("Belkin USB-C Hub (7-in-1)", "Universal hub with HDMI, USB 3.0, SD card reader, 100W power delivery", 99.99m, 30, accessoriesCategory.Id, "BLKN-HUBUSBC7"),
};

// Add all products to database
foreach (var product in computerProducts.Concat(smartphoneProducts).Concat(tabletProducts).Concat(accessoriesProducts))
{
    await dbContext.Products.AddAsync(product);
}

await dbContext.CommitAsync();


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

    for (int i = 1; i <= 21; i++)
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
    .AddInteractiveServerRenderMode();

app.Run();
