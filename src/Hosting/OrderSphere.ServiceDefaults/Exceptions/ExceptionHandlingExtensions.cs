using Microsoft.Extensions.DependencyInjection;
using OrderSphere.ServiceDefaults;

namespace Microsoft.Extensions.Hosting;

public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Registers problem-details support and the shared <see cref="ValidationExceptionHandler"/>
    /// that maps FluentValidation failures to HTTP 400. Call once per API service; pair with
    /// <c>app.UseExceptionHandler()</c> in the request pipeline.
    /// </summary>
    public static IHostApplicationBuilder AddOrderSphereExceptionHandling(this IHostApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        return builder;
    }
}
