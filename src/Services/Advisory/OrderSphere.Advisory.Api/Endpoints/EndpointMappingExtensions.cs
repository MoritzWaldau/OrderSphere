using Asp.Versioning;
using OrderSphere.Advisory.Api.Agent;

namespace OrderSphere.Advisory.Api.Endpoints;

public static class EndpointMappingExtensions
{
    public static void MapAdvisorEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var v1 = app.MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1.0)
            .MapToApiVersion(1.0);

        var advisor = v1.MapGroup("advisor");

        advisor.MapAdvisorChatEndpoints();
        advisor.MapAdvisorHistoryEndpoints();
    }
}
