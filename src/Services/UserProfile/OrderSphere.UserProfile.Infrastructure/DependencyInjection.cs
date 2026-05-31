using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderSphere.UserProfile.Application.Abstractions;
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

        return builder;
    }
}
