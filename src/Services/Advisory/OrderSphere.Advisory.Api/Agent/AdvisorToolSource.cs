using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace OrderSphere.Advisory.Api.Agent;

/// <summary>
/// Supplies the tools for one advisory turn. The lease owns the underlying
/// connection (the MCP client) and must be disposed when the turn ends.
/// </summary>
public interface IAdvisorToolSource
{
    Task<AdvisorToolLease> AcquireAsync(CancellationToken ct);
}

public sealed record AdvisorToolLease(IReadOnlyList<AITool> Tools, IAsyncDisposable? Connection)
    : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Connection?.DisposeAsync() ?? ValueTask.CompletedTask;
}

// Connects to the MCP server per request: the connection carries the *current*
// caller's bearer token so user-scoped tools resolve the correct customer.
public sealed class McpAdvisorToolSource(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : IAdvisorToolSource
{
    public async Task<AdvisorToolLease> AcquireAsync(CancellationToken ct)
    {
        var mcpBaseUrl = configuration["Services:Mcp:BaseUrl"] ?? "https://ordersphere-mcp";
        var headers = new Dictionary<string, string>();

        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            headers["Authorization"] = authorization;
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{mcpBaseUrl.TrimEnd('/')}/mcp"),
            AdditionalHeaders = headers
        });

        var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: ct);
        try
        {
            var tools = await mcpClient.ListToolsAsync(cancellationToken: ct);
            return new AdvisorToolLease([.. tools.Cast<AITool>()], mcpClient);
        }
        catch
        {
            await mcpClient.DisposeAsync();
            throw;
        }
    }
}
