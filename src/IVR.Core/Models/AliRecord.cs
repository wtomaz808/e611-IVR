using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Automatic Location Identification record — identifies the caller's location.
/// </summary>
public class AliRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public Address Address { get; set; } = new();

    [JsonPropertyName("coordinates")]
    public GeoCoordinates? Coordinates { get; set; }

    [JsonPropertyName("locationType")]
    public LocationType LocationType { get; set; } = LocationType.Unknown;

    [JsonPropertyName("serviceArea")]
    public string? ServiceArea { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "America/New_York";

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => PhoneNumber[..Math.Min(4, PhoneNumber.Length)];
}

public class Address
{
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("zipCode")]
    public string? ZipCode { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; } = "US";

    public override string ToString() =>
        $"{Street}, {City}, {State} {ZipCode}, {Country}";
}

public class GeoCoordinates
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

public enum LocationType
{
    Unknown,
    Residential,
    Commercial,
    Mobile,
    VoIP,
    Payphone
}
