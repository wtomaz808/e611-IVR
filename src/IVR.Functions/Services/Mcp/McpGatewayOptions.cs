namespace IVR.Functions.Services.Mcp;

/// <summary>
/// Binds the "Mcp" configuration section (set by <c>function-app.bicep</c> from
/// the environment's MCP server deployment).
/// </summary>
public class McpGatewayOptions
{
    /// <summary>Master switch. When false, the gateway skips MCP entirely and always uses the direct fallback.</summary>
    public bool Enabled { get; set; }

    /// <summary>MCP server base URL, e.g. https://ivr-mcp-mcp-&lt;suffix&gt;.azurewebsites.us (without the /mcp suffix).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Entra audience (API application ID URI) to request a token for.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>When true (default), MCP failures/timeouts fall back to direct service calls instead of throwing.</summary>
    public bool FallbackToDirect { get; set; } = true;

    /// <summary>Per-call timeout budget. Starts small since this sits on the live-call path — tune from telemetry.</summary>
    public int RequestTimeoutSeconds { get; set; } = 2;

    /// <summary>True when the Function App is running in an Azure Government tenant (selects the Gov authority host for token acquisition).</summary>
    public bool IsGovCloud { get; set; }
}
