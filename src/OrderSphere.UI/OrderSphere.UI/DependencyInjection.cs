using Microsoft.EntityFrameworkCore;

namespace OrderSphere.UI;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {

        return builder; 
    }
}
