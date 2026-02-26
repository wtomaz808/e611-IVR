using IVR.Core.Interfaces;
using IVR.Core.Models;

namespace IVR.AdminPortal.Services;

/// <summary>
/// Service providing dashboard analytics and summary data.
/// </summary>
public class AdminDashboardService
{
    private readonly ICosmosDbService _cosmosDb;

    public AdminDashboardService(ICosmosDbService cosmosDb)
    {
        _cosmosDb = cosmosDb;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.AddDays(-7);

        var todayCalls = await _cosmosDb.GetCallLogsAsync(todayStart, now, 1000);
        var weekCalls = await _cosmosDb.GetCallLogsAsync(weekStart, now, 1000);
        var menus = await _cosmosDb.GetAllMenusAsync();
        var prompts = await _cosmosDb.GetAllPromptsAsync();
        var businessHours = await _cosmosDb.GetBusinessHoursAsync();

        return new DashboardSummary
        {
            TodayCallCount = todayCalls.Count,
            WeekCallCount = weekCalls.Count,
            TodayAverageCallDuration = todayCalls.Where(c => c.DurationSeconds.HasValue)
                .Select(c => c.DurationSeconds!.Value).DefaultIfEmpty(0).Average(),
            ActiveMenuCount = menus.Count(m => m.IsActive),
            TotalPromptCount = prompts.Count,
            IsBusinessHoursNow = businessHours?.IsCurrentlyOpen() ?? true,
            RecentCalls = todayCalls.OrderByDescending(c => c.StartTime).Take(10).ToList(),
            CallsByDisposition = weekCalls.GroupBy(c => c.Disposition)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            CallsByHour = todayCalls.GroupBy(c => c.StartTime.Hour)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

public class DashboardSummary
{
    public int TodayCallCount { get; set; }
    public int WeekCallCount { get; set; }
    public double TodayAverageCallDuration { get; set; }
    public int ActiveMenuCount { get; set; }
    public int TotalPromptCount { get; set; }
    public bool IsBusinessHoursNow { get; set; }
    public List<CallLog> RecentCalls { get; set; } = new();
    public Dictionary<string, int> CallsByDisposition { get; set; } = new();
    public Dictionary<int, int> CallsByHour { get; set; } = new();
}
