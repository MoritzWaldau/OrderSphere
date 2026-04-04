using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using MudBlazor.Services;
using OrderSphere.Domain.Entities;
using OrderSphere.Infrastructure.Persistence;
using OrderSphere.UI.Models.Auth;
using OrderSphere.UI.Services;
using Serilog;
using System.Globalization;

namespace OrderSphere.UI;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig.ReadFrom.Configuration(context.Configuration);
        });

        return builder;
    }

    public static WebApplicationBuilder AddServiceBus(this WebApplicationBuilder builder)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
        builder.AddAzureServiceBusClient("azure-service-bus");

        return builder;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddSingleton(_ =>
        //{
        //    string connectionString = configuration.GetConnectionString("azure-service-bus") ?? throw new Exception("Unable to read connectionString: azure-service-bus");
        //    return new ServiceBusClient(connectionString);
        //});
        //

        services.AddScoped(sp =>
        {
            return new HttpClient
            {
                BaseAddress = new Uri(configuration["BaseUrl"] ?? "https://localhost:7051")
            };
        });

        services.AddAuthentication();
        services.ConfigureMudBlazor();
        return services;
    }

    private static void AddAuthentication(this IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, AuthenticationStateSerivce>();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies();

        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<OrderSphereDbContext>()
            .AddSignInManager<SignInManager<ApplicationUser>>()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsFactory>()
            .AddDefaultTokenProviders();
    }

    private static void ConfigureMudBlazor(this IServiceCollection services)
    {
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
        });
        services.Configure<PopoverOptions>(options =>
        {
            options.ThrowOnDuplicateProvider = false;
        });

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
    }

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/account/login", async (
            SignInManager<ApplicationUser> signInManager,
            [FromBody] LoginRequest request) =>
        {
            var result = await signInManager.PasswordSignInAsync(
                request.Email, request.Password, request.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded) return Results.Ok();
            if (result.IsLockedOut) return Results.BadRequest("locked");
            if (result.IsNotAllowed) return Results.BadRequest("not_allowed");
            return Results.BadRequest("invalid");
        });
    }
}
