using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.BuildingBlocks.EventBus.Outbox;
using OrderSphere.UserProfile.Application.Abstractions;
using OrderSphere.UserProfile.Infrastructure.Outbox;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.UserProfile.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddUserProfileInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<UserProfileDbContext>("userprofile-db", settings =>
        {
            settings.DisableRetry = false;
        });

        builder.Services.AddScoped<IUserProfileDbContext>(sp =>
            sp.GetRequiredService<UserProfileDbContext>());

        // Outbox: writes to DB, dispatched by OutboxDispatcher background service.
        builder.Services.AddScoped<IOutboxEventHandler, CustomerErasureRequestedEventHandler>();

        return builder;
    }

    /// <summary>
    /// Registers OutboxDispatcher and OutboxCleanupService as hosted background services.
    /// </summary>
    public static IServiceCollection AddUserProfileOutboxProcessing(this IServiceCollection services)
        => services.AddOutboxProcessing<UserProfileDbContext>();
}
