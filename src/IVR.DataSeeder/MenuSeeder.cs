using IVR.Core.Models;

namespace IVR.DataSeeder;

/// <summary>
/// Generates test menus (call flows) for E911 IVR system.
/// </summary>
public static class MenuSeeder
{
    /// <param name="webhookBaseUrl">
    /// Base URL for the simulator's mock external API. Defaults to the docker-compose
    /// service hostname; pass the deployed simulator's public URL when seeding a Cosmos DB
    /// used by a Function App that isn't running inside the same docker-compose network.
    /// </param>
    public static List<IvrMenu> GenerateTestMenus(string webhookBaseUrl = "http://pstn-simulator:8080")
    {
        var menus = new List<IvrMenu>();
        var workOrderWebhookUrl = $"{webhookBaseUrl.TrimEnd('/')}/api/external/work-order";

        // ─── Main Menu ──────────────────────────────────────────
        var mainMenu = new IvrMenu
        {
            Id = "menu-main",
            Name = "E911 Main Menu",
            Description = "Primary E911 administration line",
            PromptId = "prompt-welcome",
            MenuType = MenuType.Root,
            IsRootMenu = true,
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 0,
            Options = new List<MenuOption>
            {
                new()
                {
                    DtmfKey = "1",
                    Label = "Emergency — Transfer to RDC Dispatcher",
                    SpeechKeywords = new List<string> { "emergency", "dispatch", "urgent", "rdc" },
                    Action = new MenuAction
                    {
                        Type = ActionType.TransferToVdn,
                        VdnAddress = "sip:70200@sbc.yourdomain.com",
                        PromptId = "prompt-emergency-transfer"
                    }
                },
                new()
                {
                    DtmfKey = "2",
                    Label = "Alarm Administrator",
                    SpeechKeywords = new List<string> { "alarm", "administrator", "lamas", "monitoring" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-admin" }
                },
                new()
                {
                    DtmfKey = "3",
                    Label = "Fire Dispatcher",
                    SpeechKeywords = new List<string> { "fire", "fire dispatcher", "fed fire" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-fire-dispatch" }
                },
                new()
                {
                    DtmfKey = "4",
                    Label = "Police Dispatcher",
                    SpeechKeywords = new List<string> { "police", "police dispatcher", "joint base police" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-police-dispatch" }
                },
                new()
                {
                    DtmfKey = "5",
                    Label = "Fire Alarm AI Assistant",
                    SpeechKeywords = new List<string> { "fire alarm assistant", "ai assistant", "put alarm in test" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-fire-ai-assistant" }
                }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        // ─── Option 2: Alarm Administrator → LAMAS Area ────────
        var alarmAdminMenu = new IvrMenu
        {
            Id = "menu-alarm-admin",
            Name = "Alarm Administrator — LAMAS Area",
            Description = "Select the affected LAMAS (Local Alarm Monitoring Automatic System) area",
            PromptId = "prompt-lamas-area",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-main",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Joint Base Pearl Harbor",
                    SpeechKeywords = new List<string> { "pearl harbor", "joint base" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-select" } },
                new() { DtmfKey = "2", Label = "Hickam",
                    SpeechKeywords = new List<string> { "hickam" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-select" } },
                new() { DtmfKey = "3", Label = "West Loch",
                    SpeechKeywords = new List<string> { "west loch" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-select" } },
                new() { DtmfKey = "4", Label = "NCTAMS",
                    SpeechKeywords = new List<string> { "nctams" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-select" } },
                new() { DtmfKey = "5", Label = "PMRF",
                    SpeechKeywords = new List<string> { "pmrf" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-select" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Option 2b: Select Alarm ─────────────────────────────
        var alarmSelectMenu = new IvrMenu
        {
            Id = "menu-alarm-select",
            Name = "Alarm Administrator — Select Alarm",
            Description = "Select which alarm this is regarding",
            PromptId = "prompt-alarm-select",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-alarm-admin",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Fire Alarm",
                    SpeechKeywords = new List<string> { "fire alarm", "fire" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-status" } },
                new() { DtmfKey = "2", Label = "Intrusion Alarm",
                    SpeechKeywords = new List<string> { "intrusion", "break in", "security alarm" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-status" } },
                new() { DtmfKey = "3", Label = "Duress Alarm",
                    SpeechKeywords = new List<string> { "duress", "panic" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-status" } },
                new() { DtmfKey = "4", Label = "Supervisory Alarm",
                    SpeechKeywords = new List<string> { "supervisory", "tamper", "trouble" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-status" } },
                new() { DtmfKey = "5", Label = "Environmental Alarm",
                    SpeechKeywords = new List<string> { "environmental", "flood", "temperature", "gas" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-alarm-status" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Option 2c: Alarm Status (Inspection / Test / Maintenance) ──
        var alarmStatusMenu = new IvrMenu
        {
            Id = "menu-alarm-status",
            Name = "Alarm Administrator — Alarm Status",
            Description = "Will the alarms be in inspection, test, or maintenance?",
            PromptId = "prompt-alarm-status",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-alarm-select",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 2,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Inspection",
                    SpeechKeywords = new List<string> { "inspection", "inspect" },
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = workOrderWebhookUrl, PromptId = "prompt-alarm-submitted" } },
                new() { DtmfKey = "2", Label = "Test",
                    SpeechKeywords = new List<string> { "test", "testing" },
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = workOrderWebhookUrl, PromptId = "prompt-alarm-submitted" } },
                new() { DtmfKey = "3", Label = "Maintenance",
                    SpeechKeywords = new List<string> { "maintenance", "repair", "service" },
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = workOrderWebhookUrl, PromptId = "prompt-alarm-submitted" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Option 3: Fire Dispatcher → Department ─────────────
        var fireDispatchMenu = new IvrMenu
        {
            Id = "menu-fire-dispatch",
            Name = "Fire Dispatcher — Department",
            Description = "Select which fire department",
            PromptId = "prompt-fire-department",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-main",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 2,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Fed Fire",
                    SpeechKeywords = new List<string> { "fed fire", "federal fire" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-fire-reason" } },
                new() { DtmfKey = "2", Label = "PMRF",
                    SpeechKeywords = new List<string> { "pmrf" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-fire-reason" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Option 3b: Fire Dispatch → Reason for Call ──────
        var fireReasonMenu = new IvrMenu
        {
            Id = "menu-fire-reason",
            Name = "Fire Dispatch — Reason for Call",
            Description = "Non-emergency reason for contacting fire dispatch",
            PromptId = "prompt-fire-reason",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-fire-dispatch",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Burn Permit Request",
                    SpeechKeywords = new List<string> { "burn permit", "permit", "burning" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70220@sbc.yourdomain.com", PromptId = "prompt-transfer-fire" } },
                new() { DtmfKey = "2", Label = "Fire Prevention / Inspection",
                    SpeechKeywords = new List<string> { "prevention", "inspection", "fire inspection" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70220@sbc.yourdomain.com", PromptId = "prompt-transfer-fire" } },
                new() { DtmfKey = "3", Label = "Community Outreach / Education",
                    SpeechKeywords = new List<string> { "outreach", "education", "community", "school", "tour" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70220@sbc.yourdomain.com", PromptId = "prompt-transfer-fire" } },
                new() { DtmfKey = "4", Label = "Incident Report Request",
                    SpeechKeywords = new List<string> { "report", "incident report", "records" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70220@sbc.yourdomain.com", PromptId = "prompt-transfer-fire" } },
                new() { DtmfKey = "5", Label = "General Inquiry",
                    SpeechKeywords = new List<string> { "general", "question", "other", "inquiry" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70220@sbc.yourdomain.com", PromptId = "prompt-transfer-fire" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Option 4: Police Dispatcher → Department ───────────
        var policeDispatchMenu = new IvrMenu
        {
            Id = "menu-police-dispatch",
            Name = "Police Dispatcher — Department",
            Description = "Select which police department",
            PromptId = "prompt-police-department",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-main",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 3,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Joint Base Police",
                    SpeechKeywords = new List<string> { "joint base police", "joint base" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-police-reason" } },
                new() { DtmfKey = "2", Label = "PMRF",
                    SpeechKeywords = new List<string> { "pmrf" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-police-reason" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Option 4b: Police Dispatch → Reason for Call ──────
        var policeReasonMenu = new IvrMenu
        {
            Id = "menu-police-reason",
            Name = "Police Dispatch — Reason for Call",
            Description = "Non-emergency reason for contacting police dispatch",
            PromptId = "prompt-police-reason",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-police-dispatch",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "File a Report",
                    SpeechKeywords = new List<string> { "report", "file a report", "incident" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70230@sbc.yourdomain.com", PromptId = "prompt-transfer-police" } },
                new() { DtmfKey = "2", Label = "Traffic / Parking Concern",
                    SpeechKeywords = new List<string> { "traffic", "parking", "speeding", "vehicle" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70230@sbc.yourdomain.com", PromptId = "prompt-transfer-police" } },
                new() { DtmfKey = "3", Label = "Lost / Found Property",
                    SpeechKeywords = new List<string> { "lost", "found", "property", "missing item" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70230@sbc.yourdomain.com", PromptId = "prompt-transfer-police" } },
                new() { DtmfKey = "4", Label = "Noise Complaint",
                    SpeechKeywords = new List<string> { "noise", "complaint", "disturbance" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70230@sbc.yourdomain.com", PromptId = "prompt-transfer-police" } },
                new() { DtmfKey = "5", Label = "General Inquiry",
                    SpeechKeywords = new List<string> { "general", "question", "other", "inquiry" },
                    Action = new MenuAction { Type = ActionType.TransferToVdn, VdnAddress = "sip:70230@sbc.yourdomain.com", PromptId = "prompt-transfer-police" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        var afterHoursMenu = new IvrMenu
        {
            Id = "menu-afterhours",
            Name = "After Hours (E911)",
            Description = "After hours — emergencies still transfer to RDC",
            PromptId = "prompt-afterhours",
            MenuType = MenuType.AfterHours,
            IsActive = true,
            Order = 10,
            MaxRetriesExceededAction = new MenuAction { Type = ActionType.Hangup, PromptId = "prompt-goodbye" },
            Options = new List<MenuOption>
            {
                new()
                {
                    DtmfKey = "1",
                    Label = "Emergency — Transfer to RDC",
                    SpeechKeywords = new List<string> { "emergency", "fire", "dispatch" },
                    Action = new MenuAction
                    {
                        Type = ActionType.TransferToVdn,
                        VdnAddress = "sip:70200@sbc.yourdomain.com",
                        PromptId = "prompt-emergency-transfer"
                    }
                },
                new()
                {
                    DtmfKey = "2",
                    Label = "Leave Voicemail",
                    Action = new MenuAction { Type = ActionType.Voicemail }
                }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var speechMenu = new IvrMenu
        {
            Id = "menu-speech",
            Name = "Speech Routing (E911)",
            Description = "AI-powered speech routing for fire alarm admin",
            PromptId = "prompt-speech",
            MenuType = MenuType.SpeechRouting,
            IsActive = true,
            EnableSpeechRecognition = true,
            SpeechRoutingPromptId = "prompt-speech",
            TeamRoutingConfigIds = new List<string> { "team-rdc", "team-alarm-admin", "team-fire-dispatch", "team-police-dispatch" },
            SpeechFallbackAction = new MenuAction
            {
                Type = ActionType.TransferToVdn,
                VdnAddress = "sip:70300@sbc.yourdomain.com"
            },
            Order = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // ─── Option 5: Fire Alarm AI Assistant (speech → AI data extraction → external system) ──
        var fireAiAssistantMenu = new IvrMenu
        {
            Id = "menu-fire-ai-assistant",
            Name = "Fire Alarm AI Assistant",
            Description = "Caller describes a fire alarm action in natural language; AI extracts fields and submits to the fire alarm panel",
            MenuType = MenuType.SpeechRouting,
            ParentMenuId = "menu-main",
            IsActive = true,
            EnableSpeechRecognition = true,
            SpeechRoutingPromptId = "prompt-fire-ai-describe",
            // No teams configured — this menu always falls through to SpeechFallbackAction,
            // which runs the DataExtractionConfig-driven SubmitToExternalSystem pipeline.
            TeamRoutingConfigIds = new List<string>(),
            SpeechFallbackAction = new MenuAction
            {
                Type = ActionType.SubmitToExternalSystem,
                DataExtractionConfigId = "extract-fire-alarm-001"
            },
            Order = 6,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        menus.AddRange(new[]
        {
            mainMenu, alarmAdminMenu, alarmSelectMenu, alarmStatusMenu,
            fireDispatchMenu, fireReasonMenu,
            policeDispatchMenu, policeReasonMenu,
            afterHoursMenu, speechMenu, fireAiAssistantMenu
        });

        return menus;
    }
}
