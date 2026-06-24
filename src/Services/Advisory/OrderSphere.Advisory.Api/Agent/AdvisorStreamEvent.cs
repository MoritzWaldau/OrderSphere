namespace OrderSphere.Advisory.Api.Agent;

public enum AdvisorStreamEventKind
{
    /// <summary>A token of assistant text to append to the reply.</summary>
    Text,

    /// <summary>A status notice that the agent is invoking a tool; Text carries a user-facing label.</summary>
    Tool,

    /// <summary>
    /// A write-action requires customer confirmation before execution.
    /// Text carries the raw confirmation_required JSON payload from the tool.
    /// </summary>
    Confirm
}

public sealed record AdvisorStreamEvent(AdvisorStreamEventKind Kind, string Text)
{
    public static AdvisorStreamEvent OfText(string text) => new(AdvisorStreamEventKind.Text, text);
    public static AdvisorStreamEvent OfTool(string label) => new(AdvisorStreamEventKind.Tool, label);
    public static AdvisorStreamEvent OfConfirm(string payload) => new(AdvisorStreamEventKind.Confirm, payload);
}
