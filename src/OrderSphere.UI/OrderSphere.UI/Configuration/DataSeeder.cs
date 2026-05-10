using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderSphere.Domain.Entities;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.UI.Configuration;

public static class DataSeeder
{
    public static async Task SeedDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderSphereDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Apply pending migrations (replaces EnsureCreated)
        await dbContext.Database.MigrateAsync();

        await SeedCategoriesAndProductsAsync(dbContext);
        await SeedRolesAndUsersAsync(scope, configuration);
    }

    private static async Task SeedCategoriesAndProductsAsync(OrderSphereDbContext dbContext)
    {
        if (await dbContext.Categories.AnyAsync())
        {
            return; // Already seeded
        }

        await dbContext.BeginTransactionAsync();

        var computerCategory = new Category("Computer", "Laptops, Desktops und Computer");
        var smartphoneCategory = new Category("Smartphones", "Mobile Geräte und Smartphones");
        var tabletCategory = new Category("Tablets", "Tablet-Computer und iPad");
        var accessoriesCategory = new Category("Accessories", "Zubehör und Erweiterungen");

        await dbContext.Categories.AddAsync(computerCategory);
        await dbContext.Categories.AddAsync(smartphoneCategory);
        await dbContext.Categories.AddAsync(tabletCategory);
        await dbContext.Categories.AddAsync(accessoriesCategory);

        await dbContext.SaveChangesAsync();

        var computerProducts = new List<Product>
        {
            new("MacBook Pro 16\" M4 Max", "High-performance laptop with Apple M4 Max chip, 16GB RAM, 512GB SSD, Retina display", 2499.99m, 8, computerCategory.Id, "APPL-MBP16-M4-MAX"),
            new("MacBook Air 15\" M3", "Ultrabook with M3 chip, 8GB RAM, 256GB SSD, ideal for professionals", 1499.99m, 12, computerCategory.Id, "APPL-MBA15-M3"),
            new("Mac Mini M4", "Compact desktop computer with M4 chip, 16GB RAM, 512GB SSD, perfect for home office", 899.99m, 15, computerCategory.Id, "APPL-MINI-M4"),
            new("Samsung Galaxy Book4 Pro", "Windows Laptop with Intel i7, 16GB RAM, 512GB SSD, AMOLED display, 6-hour battery", 1799.99m, 10, computerCategory.Id, "SAMS-BOOK4-I7"),
            new("Samsung Galaxy Book4", "Windows Laptop with Intel i5, 8GB RAM, 256GB SSD, FHD display, lightweight design", 999.99m, 14, computerCategory.Id, "SAMS-BOOK4-I5"),
        };

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

        foreach (var product in computerProducts.Concat(smartphoneProducts).Concat(tabletProducts).Concat(accessoriesProducts))
        {
            await dbContext.Products.AddAsync(product);
        }

        await dbContext.CommitAsync();
    }

    private static async Task SeedRolesAndUsersAsync(IServiceScope scope, IConfiguration configuration)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        const string adminRoleName = "Administrator";
        const string userRoleName = "User";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
            await roleManager.CreateAsync(new IdentityRole(adminRoleName));

        if (!await roleManager.RoleExistsAsync(userRoleName))
            await roleManager.CreateAsync(new IdentityRole(userRoleName));

        var adminEmail = configuration["Seed:AdminEmail"]
            ?? throw new InvalidOperationException("Seed:AdminEmail is not configured. Set it via user-secrets: dotnet user-secrets set \"Seed:AdminEmail\" \"...\"");
        var adminPassword = configuration["Seed:AdminPassword"]
            ?? throw new InvalidOperationException("Seed:AdminPassword is not configured. Set it via user-secrets: dotnet user-secrets set \"Seed:AdminPassword\" \"...\"");
        var userEmail = configuration["Seed:UserEmail"]
            ?? throw new InvalidOperationException("Seed:UserEmail is not configured. Set it via user-secrets: dotnet user-secrets set \"Seed:UserEmail\" \"...\"");
        var userPassword = configuration["Seed:UserPassword"]
            ?? throw new InvalidOperationException("Seed:UserPassword is not configured. Set it via user-secrets: dotnet user-secrets set \"Seed:UserPassword\" \"...\"");

        await EnsureUserAsync(userManager, adminEmail, "Admin", "User", adminPassword, adminRoleName);
        await EnsureUserAsync(userManager, userEmail, "Test", "User", userPassword, userRoleName);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        string password,
        string roleName)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, roleName))
            {
                await userManager.AddToRoleAsync(existing, roleName);
            }
            return;
        }

        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
