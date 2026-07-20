using IVR.Core.Models;

namespace IVR.DataSeeder;

/// <summary>
/// Generates seed PhoneNumberConfig entries for the E911 IVR system.
/// These records map inbound DIDs to their root IVR menus and PSTN connection type.
///
/// Teams mode (PstnNumberType.TeamsBot): numbers are assigned to Teams Resource Accounts.
/// Calls arrive via Teams Phone System (Calling Plans or Operator Connect) and are
/// delivered to the IVR as Graph commsNotifications on the /api/bot-messages endpoint.
/// </summary>
public static class PhoneNumberSeeder
{
    public static List<PhoneNumberConfig> GeneratePhoneNumberConfigs()
    {
        return new List<PhoneNumberConfig>
        {
            // ─── E911 Main Line ──────────────────────────────────────────────────
            // Primary E911 administration line — routes to the main IVR menu.
            // Teams Resource Account: e911-main@tenant.onmicrosoft.us
            new()
            {
                Id           = "pn-e911-main",
                PhoneNumber  = "+17035550911",
                Label        = "E911 Main Line",
                NumberType   = PstnNumberType.TeamsBot,
                RootMenuId   = "menu-main",
                IsActive     = true
            },

            // ─── Fire Dispatch Direct Line ───────────────────────────────────────
            // Direct line for fire dispatch — skips the main menu.
            // Teams Resource Account: e911-fire@tenant.onmicrosoft.us
            new()
            {
                Id           = "pn-fire-dispatch",
                PhoneNumber  = "+17035550119",
                Label        = "Fire Dispatch Direct",
                NumberType   = PstnNumberType.TeamsBot,
                RootMenuId   = "menu-fire-dispatch",
                IsActive     = true
            },

            // ─── Police Dispatch Direct Line ────────────────────────────────────
            // Direct line for police dispatch — skips the main menu.
            new()
            {
                Id           = "pn-police-dispatch",
                PhoneNumber  = "+17035550110",
                Label        = "Police Dispatch Direct",
                NumberType   = PstnNumberType.TeamsBot,
                RootMenuId   = "menu-police-dispatch",
                IsActive     = true
            },

            // ─── Admin / Test Line ───────────────────────────────────────────────
            // Low-traffic line for IVR testing and admin calls.
            new()
            {
                Id           = "pn-admin-test",
                PhoneNumber  = "+17035559999",
                Label        = "Admin / Test Line",
                NumberType   = PstnNumberType.TeamsBot,
                RootMenuId   = "menu-main",
                IsActive     = true
            }
        };
    }
}


/// <summary>
/// Generates seed PhoneNumberConfig entries for the E911 IVR system.
/// These records map inbound DIDs to their root IVR menus and PSTN connection type.
///
/// Teams mode (PstnNumberType.TeamsBot): numbers are assigned to Teams Resource Accounts.
/// Calls arrive via Teams Phone System (Calling Plans or Operator Connect) and are
/// delivered to the IVR as Graph commsNotifications on the /api/bot-messages endpoint.
/// </summary>
