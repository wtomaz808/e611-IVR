using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Extensions.Logging;

namespace IVR.Functions.Services;

/// <summary>
/// IVR call flow engine — resolves menus and processes input based on ANI/ALI and conditions.
/// </summary>
public class CallFlowEngine : ICallFlowEngine
{
    private readonly ICosmosDbService _cosmosDb;
    private readonly AniAliService _aniAliService;
    private readonly ILogger<CallFlowEngine> _logger;

    public CallFlowEngine(ICosmosDbService cosmosDb, AniAliService aniAliService, ILogger<CallFlowEngine> logger)
    {
        _cosmosDb = cosmosDb;
        _aniAliService = aniAliService;
        _logger = logger;
    }

    /// <summary>
    /// Build full call context including ANI/ALI data and business hours status.
    /// </summary>
    public async Task<CallContext> BuildCallContextAsync(string callerNumber, string calledNumber)
    {
        var (ani, ali) = await _aniAliService.LookupCallerAsync(callerNumber);
        var isBusinessHours = await IsWithinBusinessHoursAsync();
        var rootMenu = await ResolveMenuAsync(callerNumber, calledNumber);

        return new CallContext
        {
            CallId = Guid.NewGuid().ToString(),
            CallerNumber = callerNumber,
            CalledNumber = calledNumber,
            AniData = ani,
            AliData = ali,
            IsBusinessHours = isBusinessHours,
            IsVip = ani?.IsVip ?? false,
            IsBlocked = ani?.IsBlocked ?? false,
            Language = ani?.Language ?? "en-US",
            CurrentMenu = rootMenu,
            StartTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Resolve which menu to show the caller based on the called DID, conditions, ANI/ALI, and business hours.
    /// When calledNumber is provided, the engine first checks the PhoneNumbers collection for a per-DID root menu.
    /// </summary>
    public async Task<IvrMenu> ResolveMenuAsync(string callerNumber, string? calledNumber = null, string? currentMenuId = null)
    {
        // If a specific menu is requested, return it
        if (currentMenuId != null)
        {
            var menu = await _cosmosDb.GetMenuAsync(currentMenuId);
            if (menu != null && menu.IsActive)
                return menu;
        }

        // ─── Per-DID routing: look up the called number in PhoneNumbers ───
        PhoneNumberConfig? phoneConfig = null;
        if (!string.IsNullOrEmpty(calledNumber) && calledNumber != "unknown")
        {
            phoneConfig = await _cosmosDb.GetPhoneNumberConfigAsync(calledNumber);
            if (phoneConfig?.IsActive == true && phoneConfig.RootMenuId != null)
            {
                var didMenu = await _cosmosDb.GetMenuAsync(phoneConfig.RootMenuId);
                if (didMenu != null && didMenu.IsActive)
                {
                    _logger.LogInformation("Per-DID routing: {CalledNumber} → menu {MenuName}", calledNumber, didMenu.Name);
                    // Still apply ANI/ALI conditions on top of the per-DID root menu
                    var (aniForConditions, aliForConditions) = await _aniAliService.LookupCallerAsync(callerNumber);
                    var conditionMenu = await EvaluateConditionsAsync(didMenu, aniForConditions, aliForConditions);
                    return conditionMenu ?? didMenu;
                }
            }
        }

        // Check for custom ANI-based routing
        var (ani, ali) = await _aniAliService.LookupCallerAsync(callerNumber);

        if (ani?.IsBlocked == true)
        {
            _logger.LogWarning("Blocked caller {PhoneNumber} attempted to reach IVR", callerNumber);
            // Return a "blocked" menu or throw
        }

        // VIP routing
        if (ani?.IsVip == true && ani.CustomRoutingMenuId != null)
        {
            var vipMenu = await _cosmosDb.GetMenuAsync(ani.CustomRoutingMenuId);
            if (vipMenu != null && vipMenu.IsActive)
            {
                _logger.LogInformation("VIP caller {PhoneNumber} routed to {MenuName}", callerNumber, vipMenu.Name);
                return vipMenu;
            }
        }

        // Check business hours
        var isBusinessHours = await IsWithinBusinessHoursAsync();
        if (!isBusinessHours)
        {
            var businessHours = await _cosmosDb.GetBusinessHoursAsync();
            if (businessHours?.AfterHoursMenuId != null)
            {
                var afterHoursMenu = await _cosmosDb.GetMenuAsync(businessHours.AfterHoursMenuId);
                if (afterHoursMenu != null && afterHoursMenu.IsActive)
                {
                    _logger.LogInformation("After-hours routing to {MenuName}", afterHoursMenu.Name);
                    return afterHoursMenu;
                }
            }
        }

        // Check for holiday
        var bhConfig = await _cosmosDb.GetBusinessHoursAsync();
        if (bhConfig != null)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(bhConfig.Timezone);
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var todayHoliday = bhConfig.Holidays.FirstOrDefault(h => h.Date.Date == now.Date);
            if (todayHoliday != null && todayHoliday.UseHolidayMenu && bhConfig.HolidayMenuId != null)
            {
                var holidayMenu = await _cosmosDb.GetMenuAsync(bhConfig.HolidayMenuId);
                if (holidayMenu != null && holidayMenu.IsActive)
                {
                    _logger.LogInformation("Holiday routing to {MenuName} for {Holiday}", holidayMenu.Name, todayHoliday.Name);
                    return holidayMenu;
                }
            }
        }

        // Default: return root menu
        var rootMenu = await _cosmosDb.GetRootMenuAsync();
        if (rootMenu == null)
        {
            _logger.LogError("No root menu configured! IVR will fail.");
            throw new InvalidOperationException("No root IVR menu is configured. Please set up the IVR in the admin portal.");
        }

        // Evaluate menu conditions
        rootMenu = await EvaluateConditionsAsync(rootMenu, ani, ali) ?? rootMenu;

        return rootMenu;
    }

    /// <summary>
    /// Process DTMF or speech input for the current menu.
    /// </summary>
    public async Task<MenuAction> ProcessInputAsync(string menuId, string input)
    {
        var menu = await _cosmosDb.GetMenuAsync(menuId)
            ?? throw new InvalidOperationException($"Menu {menuId} not found");

        // Find matching option by DTMF key
        var option = menu.Options.FirstOrDefault(o =>
            o.DtmfKey.Equals(input, StringComparison.OrdinalIgnoreCase));

        // If no DTMF match, try speech keywords
        if (option == null)
        {
            option = menu.Options.FirstOrDefault(o =>
                o.SpeechKeywords.Any(kw =>
                    input.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        if (option == null)
        {
            _logger.LogInformation("Invalid input '{Input}' for menu {MenuId}", input, menuId);
            return new MenuAction
            {
                Type = ActionType.RepeatMenu,
                PromptId = menu.InvalidInputPromptId,
                TargetMenuId = menuId
            };
        }

        _logger.LogInformation("Input '{Input}' matched option '{Label}' in menu {MenuId}",
            input, option.Label, menuId);

        return option.Action;
    }

    /// <summary>
    /// Check if the current time falls within configured business hours.
    /// </summary>
    public async Task<bool> IsWithinBusinessHoursAsync()
    {
        var businessHours = await _cosmosDb.GetBusinessHoursAsync();
        if (businessHours == null)
        {
            _logger.LogWarning("No business hours configured, defaulting to open");
            return true;
        }

        return businessHours.IsCurrentlyOpen();
    }

    /// <summary>
    /// Evaluate conditional routing rules on a menu.
    /// </summary>
    private async Task<IvrMenu?> EvaluateConditionsAsync(IvrMenu menu, AniRecord? ani, AliRecord? ali)
    {
        foreach (var condition in menu.Conditions)
        {
            var matches = condition.Type switch
            {
                ConditionType.CallerType => ani?.CallerType.ToString()
                    .Equals(condition.Value, StringComparison.OrdinalIgnoreCase) ?? false,
                ConditionType.VipStatus => ani?.IsVip.ToString()
                    .Equals(condition.Value, StringComparison.OrdinalIgnoreCase) ?? false,
                ConditionType.AliRegion => ali?.Region
                    ?.Equals(condition.Value, StringComparison.OrdinalIgnoreCase) ?? false,
                ConditionType.BusinessHours => (await IsWithinBusinessHoursAsync()).ToString()
                    .Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };

            if (matches && condition.TargetMenuId != null)
            {
                var targetMenu = await _cosmosDb.GetMenuAsync(condition.TargetMenuId);
                if (targetMenu != null && targetMenu.IsActive)
                {
                    _logger.LogInformation("Condition {ConditionType}={Value} matched, routing to {MenuName}",
                        condition.Type, condition.Value, targetMenu.Name);
                    return targetMenu;
                }
            }
        }

        return null;
    }
}
