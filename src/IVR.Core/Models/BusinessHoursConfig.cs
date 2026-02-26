using System.Text.Json.Serialization;

namespace IVR.Core.Models;

/// <summary>
/// Business hours configuration for conditional IVR routing.
/// </summary>
public class BusinessHoursConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Default";

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "America/New_York";

    [JsonPropertyName("schedule")]
    public Dictionary<DayOfWeek, DaySchedule> Schedule { get; set; } = new()
    {
        [DayOfWeek.Monday] = new DaySchedule { IsOpen = true, OpenTime = "08:00", CloseTime = "17:00" },
        [DayOfWeek.Tuesday] = new DaySchedule { IsOpen = true, OpenTime = "08:00", CloseTime = "17:00" },
        [DayOfWeek.Wednesday] = new DaySchedule { IsOpen = true, OpenTime = "08:00", CloseTime = "17:00" },
        [DayOfWeek.Thursday] = new DaySchedule { IsOpen = true, OpenTime = "08:00", CloseTime = "17:00" },
        [DayOfWeek.Friday] = new DaySchedule { IsOpen = true, OpenTime = "08:00", CloseTime = "17:00" },
        [DayOfWeek.Saturday] = new DaySchedule { IsOpen = false },
        [DayOfWeek.Sunday] = new DaySchedule { IsOpen = false }
    };

    [JsonPropertyName("holidays")]
    public List<Holiday> Holidays { get; set; } = new();

    [JsonPropertyName("afterHoursMenuId")]
    public string? AfterHoursMenuId { get; set; }

    [JsonPropertyName("holidayMenuId")]
    public string? HolidayMenuId { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey => "config";

    public bool IsCurrentlyOpen()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(Timezone);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        // Check holidays first
        if (Holidays.Any(h => h.Date.Date == now.Date))
            return false;

        if (!Schedule.TryGetValue(now.DayOfWeek, out var daySchedule))
            return false;

        if (!daySchedule.IsOpen)
            return false;

        var openTime = TimeSpan.Parse(daySchedule.OpenTime ?? "08:00");
        var closeTime = TimeSpan.Parse(daySchedule.CloseTime ?? "17:00");

        return now.TimeOfDay >= openTime && now.TimeOfDay <= closeTime;
    }
}

public class DaySchedule
{
    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }

    [JsonPropertyName("openTime")]
    public string? OpenTime { get; set; }

    [JsonPropertyName("closeTime")]
    public string? CloseTime { get; set; }
}

public class Holiday
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("useHolidayMenu")]
    public bool UseHolidayMenu { get; set; } = true;
}
