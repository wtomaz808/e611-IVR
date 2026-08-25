using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// A discrete, idempotent event recorded against a call (e.g. a menu action, an external
/// system submission, an escalation). Distinct from <see cref="CallLog"/>, which is the
/// one-document-per-call summary — this is the append-only detail trail backing the
/// "record_call_event" MCP tool. Id = eventId, partitioned by callId, so a duplicate
/// record_call_event request for the same eventId is a Cosmos create-conflict rather than
/// a silent double-write.
/// </summary>
public class CallEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("details")]
    public Dictionary<string, string> Details { get; set; } = new();

    [JsonPropertyName("recordedAt")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => CallId;
}
