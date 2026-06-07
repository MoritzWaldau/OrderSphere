namespace OrderSphere.Mcp.Server.Tools;

// Shared message returned by user-scoped tools when the MCP call carries no caller
// token. Surfaced to the agent (not the end user), which phrases the sign-in prompt.
public static class UserToolGuard
{
    public const string AuthRequired =
        "Authentication required. The caller is not signed in, so account-specific data is unavailable.";
}
