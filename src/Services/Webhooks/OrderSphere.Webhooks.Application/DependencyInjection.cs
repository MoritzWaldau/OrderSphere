using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.BuildingBlocks.Behaviors;

namespace OrderSphere.Webhooks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddWebhooksApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddTransient(typeof(INotificationHandler<>), typeof(DomainEventLoggingHandler<>));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
