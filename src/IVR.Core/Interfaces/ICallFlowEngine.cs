using IVR.Core.Models;

namespace IVR.Core.Interfaces;

public interface ICallFlowEngine
{
    Task<IvrMenu> ResolveMenuAsync(string callerNumber, string? calledNumber = null, string? currentMenuId = null);
    Task<MenuAction> ProcessInputAsync(string menuId, string input);
    Task<bool> IsWithinBusinessHoursAsync();
    Task<CallContext> BuildCallContextAsync(string callerNumber, string calledNumber);
}

public class CallContext
{
    public string CallId { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string CalledNumber { get; set; } = string.Empty;
    public AniRecord? AniData { get; set; }
    public AliRecord? AliData { get; set; }
    public bool IsBusinessHours { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlocked { get; set; }
    public string Language { get; set; } = "en-US";
    public IvrMenu CurrentMenu { get; set; } = null!;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
}
