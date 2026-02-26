using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Defines an external system that the IVR can push data to (e.g., fire alarm panel,
/// CAD system, records management system, work order platform).
/// </summary>
public class ExternalSystemConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name for the external system (e.g. "Fire Alarm Panel", "CAD System").
    /// </summary>
    [JsonPropertyName("systemName")]
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this system does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The type of integration.
    /// </summary>
    [JsonPropertyName("systemType")]
    public ExternalSystemType SystemType { get; set; } = ExternalSystemType.RestApi;

    /// <summary>
    /// Base URL for the external system's API (e.g. "https://firealarm.example.com/api/v1").
    /// </summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Available endpoints/actions on this system.
    /// </summary>
    [JsonPropertyName("endpoints")]
    public List<ExternalSystemEndpoint> Endpoints { get; set; } = new();

    /// <summary>
    /// Authentication method for this system.
    /// </summary>
    [JsonPropertyName("authType")]
    public ExternalAuthType AuthType { get; set; } = ExternalAuthType.None;

    /// <summary>
    /// Authentication configuration (key names, header names, credentials reference).
    /// Sensitive values should reference Key Vault secrets, not store plain text.
    /// </summary>
    [JsonPropertyName("authConfig")]
    public ExternalAuthConfig AuthConfig { get; set; } = new();

    /// <summary>
    /// Custom HTTP headers to include on every request to this system.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Retry policy for failed requests.
    /// </summary>
    [JsonPropertyName("retryPolicy")]
    public RetryPolicy RetryPolicy { get; set; } = new();

    /// <summary>
    /// Timeout in seconds for HTTP requests to this system.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "external-system";
}

/// <summary>
/// A specific endpoint/action available on an external system.
/// </summary>
public class ExternalSystemEndpoint
{
    /// <summary>
    /// Unique name for this action (e.g. "put-in-test", "take-out-of-test", "create-work-order").
    /// </summary>
    [JsonPropertyName("actionName")]
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this action does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, PATCH, DELETE).
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// URL path appended to the base URL. Supports {{placeholder}} tokens that are
    /// replaced with extracted data fields (e.g. "/alarms/{{locationId}}/test-mode").
    /// </summary>
    [JsonPropertyName("urlPath")]
    public string UrlPath { get; set; } = string.Empty;

    /// <summary>
    /// JSON payload template with {{placeholder}} tokens replaced at runtime
    /// with extracted data fields. Example:
    /// { "action": "{{action}}", "building": "{{location}}", "requestedBy": "{{callerNumber}}" }
    /// </summary>
    [JsonPropertyName("payloadTemplate")]
    public string? PayloadTemplate { get; set; }

    /// <summary>
    /// Optional: specific content type for the request body.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "application/json";

    /// <summary>
    /// JSON path expression to extract a confirmation/reference value from the response
    /// (e.g. "$.ticketId" or "$.data.referenceNumber").
    /// </summary>
    [JsonPropertyName("responseConfirmationField")]
    public string? ResponseConfirmationField { get; set; }

    /// <summary>
    /// TTS template to read back confirmation to the caller.
    /// Supports {{placeholder}} tokens including {{confirmationValue}}.
    /// Example: "Your reference number is {{confirmationValue}}."
    /// </summary>
    [JsonPropertyName("confirmationMessageTemplate")]
    public string? ConfirmationMessageTemplate { get; set; }
}

/// <summary>
/// Authentication configuration for external system connections.
/// </summary>
public class ExternalAuthConfig
{
    /// <summary>
    /// For ApiKey auth: the header name (e.g. "X-API-Key", "api-key").
    /// </summary>
    [JsonPropertyName("apiKeyHeaderName")]
    public string? ApiKeyHeaderName { get; set; }

    /// <summary>
    /// For ApiKey auth: the key value or Key Vault reference.
    /// </summary>
    [JsonPropertyName("apiKeyValue")]
    public string? ApiKeyValue { get; set; }

    /// <summary>
    /// For Bearer auth: the static token or Key Vault reference.
    /// </summary>
    [JsonPropertyName("bearerToken")]
    public string? BearerToken { get; set; }

    /// <summary>
    /// For BasicAuth: the username.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// For BasicAuth: the password or Key Vault reference.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

/// <summary>
/// Retry policy for external system HTTP requests.
/// </summary>
public class RetryPolicy
{
    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("initialDelayMs")]
    public int InitialDelayMs { get; set; } = 1000;

    [JsonPropertyName("backoffMultiplier")]
    public double BackoffMultiplier { get; set; } = 2.0;
}

public enum ExternalSystemType
{
    RestApi,
    Webhook,
    ServiceBus
}

public enum ExternalAuthType
{
    None,
    ApiKey,
    BearerToken,
    BasicAuth
}
