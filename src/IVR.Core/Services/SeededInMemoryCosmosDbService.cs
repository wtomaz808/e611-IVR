using IVR.Core.Interfaces;
using IVR.Core.Models;
using System.Collections.Concurrent;

namespace IVR.Core.Services;

/// <summary>
/// In-memory ICosmosDbService pre-loaded with realistic seed data.
/// Used when Cosmos DB is not configured (local/Docker dev mode).
/// All data is stored in ConcurrentDictionary for thread safety.
/// </summary>
public class SeededInMemoryCosmosDbService : ICosmosDbService
{
    private readonly ConcurrentDictionary<string, IvrPrompt> _prompts = new();
    private readonly ConcurrentDictionary<string, IvrMenu> _menus = new();
    private readonly ConcurrentDictionary<string, AniRecord> _aniRecords = new();
    private readonly ConcurrentDictionary<string, AliRecord> _aliRecords = new();
    private readonly ConcurrentDictionary<string, CallLog> _callLogs = new();
    private readonly ConcurrentDictionary<string, TeamRoutingConfig> _teamRouting = new();
    private readonly ConcurrentDictionary<string, ExternalSystemConfig> _externalSystems = new();
    private readonly ConcurrentDictionary<string, DataExtractionConfig> _dataExtraction = new();
    private readonly ConcurrentDictionary<string, PhoneNumberConfig> _phoneNumbers = new();
    private readonly ConcurrentDictionary<string, BusinessHoursConfig> _businessHours = new();
    private SystemConfig _systemConfig;

    public SeededInMemoryCosmosDbService()
    {
        _systemConfig = new SystemConfig();
        Seed();
    }

    // ─── Seed Data ──────────────────────────────────────────────

    private void Seed()
    {
        SeedPrompts();
        SeedMenus();
        SeedAniRecords();
        SeedAliRecords();
        SeedCallLogs();
        SeedTeamRouting();
        SeedPhoneNumbers();
        SeedBusinessHours();
        SeedSystemConfig();
    }

    private void SeedPrompts()
    {
        var prompts = new List<IvrPrompt>
        {
            new()
            {
                Id = "prompt-welcome",
                Name = "E911 Admin Line Welcome",
                Description = "Main E911 admin line welcome",
                Type = PromptType.Tts,
                TtsText = "Thank you for calling the E 9 1 1 administration line. If this is an emergency, press 1 to be transferred to the R D C dispatcher immediately. To reach an Alarm Administrator, press 2. To reach a Fire Dispatcher, press 3. To reach a Police Dispatcher, press 4.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Greeting",
                Tags = new List<string> { "e911", "welcome", "admin" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-emergency-transfer",
                Name = "Emergency — RDC Transfer",
                Description = "Played before transferring to Remote Dispatch Center",
                Type = PromptType.Tts,
                TtsText = "You are being transferred to the Remote Dispatch Center. Please stay on the line.",
                TtsVoice = "en-US-GuyNeural",
                Language = "en-US",
                Category = "Emergency",
                Tags = new List<string> { "emergency", "rdc", "dispatch", "transfer" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-lamas-area",
                Name = "LAMAS Area Selection",
                Description = "Alarm Administrator — select affected LAMAS area",
                Type = PromptType.Tts,
                TtsText = "Choose the affected LAMAS area. For Joint Base Pearl Harbor, press 1. For Hickam, press 2. For West Loch, press 3. For N C TAMS, press 4. For P M R F, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "lamas", "alarm", "administrator" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-fire-department",
                Name = "Fire Dispatcher Department",
                Description = "Fire Dispatcher — select department",
                Type = PromptType.Tts,
                TtsText = "Which fire department? For Fed Fire, press 1. For P M R F, press 2.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "fire", "dispatcher", "department" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-police-department",
                Name = "Police Dispatcher Department",
                Description = "Police Dispatcher — select department",
                Type = PromptType.Tts,
                TtsText = "Which police department? For Joint Base Police, press 1. For P M R F, press 2.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "police", "dispatcher", "department" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-alarm-select",
                Name = "Alarm Selection",
                Description = "Alarm Administrator — select which alarm",
                Type = PromptType.Tts,
                TtsText = "Which alarm is this regarding? For Fire Alarm, press 1. For Intrusion Alarm, press 2. For Duress Alarm, press 3. For Supervisory Alarm, press 4. For Environmental Alarm, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "alarm", "select", "type" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-alarm-status",
                Name = "Alarm Status Selection",
                Description = "Alarm Administrator — will the alarms be in inspection, test, or maintenance?",
                Type = PromptType.Tts,
                TtsText = "Will this alarm be in inspection, test, or maintenance? For Inspection, press 1. For Test, press 2. For Maintenance, press 3.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "alarm", "status", "inspection", "test", "maintenance" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-alarm-submitted",
                Name = "Alarm Information Submitted",
                Description = "Confirmation that alarm information has been submitted",
                Type = PromptType.Tts,
                TtsText = "Thank you. Your information has been submitted. Goodbye.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Confirmation",
                Tags = new List<string> { "alarm", "submitted", "confirmation" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = "prompt-fire-reason",
                Name = "Fire Dispatch — Reason for Call",
                Description = "Non-emergency fire dispatch — reason for call",
                Type = PromptType.Tts,
                TtsText = "This is the non-emergency fire line. How can we help? For a burn permit request, press 1. For fire prevention or inspection, press 2. For community outreach or education, press 3. For an incident report request, press 4. For a general inquiry, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "fire", "non-emergency", "reason" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-police-reason",
                Name = "Police Dispatch — Reason for Call",
                Description = "Non-emergency police dispatch — reason for call",
                Type = PromptType.Tts,
                TtsText = "This is the non-emergency police line. How can we help? To file a report, press 1. For a traffic or parking concern, press 2. For lost or found property, press 3. For a noise complaint, press 4. For a general inquiry, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "police", "non-emergency", "reason" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-transfer-fire",
                Name = "Transfer to Fire Dispatcher",
                Description = "Played before transferring to a fire dispatcher",
                Type = PromptType.Tts,
                TtsText = "Please hold while we connect you to the Fire Dispatcher.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Transfer",
                Tags = new List<string> { "transfer", "fire", "dispatcher" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = "prompt-transfer-police",
                Name = "Transfer to Police Dispatcher",
                Description = "Played before transferring to a police dispatcher",
                Type = PromptType.Tts,
                TtsText = "Please hold while we connect you to the Police Dispatcher.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Transfer",
                Tags = new List<string> { "transfer", "police", "dispatcher" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new()
            {
                Id = "prompt-hold",
                Name = "Hold Message",
                Description = "Message played while caller is on hold",
                Type = PromptType.Tts,
                TtsText = "Please hold while we connect you. A representative will be with you shortly. As a reminder, if this is a fire emergency, please hang up and dial 9 1 1.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Hold",
                Tags = new List<string> { "hold", "transfer" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-afterhours",
                Name = "After Hours",
                Description = "After hours greeting — emergency calls still route to RDC",
                Type = PromptType.Tts,
                TtsText = "Thank you for calling the E 9 1 1 administration line. Our office is currently closed. Business hours are Monday through Friday, 7 AM to 6 PM Eastern Time. If this is an emergency, press 1 to be transferred to the Remote Dispatch Center. Otherwise, please call back during business hours or leave a message after the tone.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Greeting",
                Tags = new List<string> { "afterhours", "e911" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-goodbye",
                Name = "Goodbye",
                Description = "Call ending message",
                Type = PromptType.Tts,
                TtsText = "Thank you for calling. If you experience a fire alarm issue, do not hesitate to call back. Goodbye.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "System",
                Tags = new List<string> { "goodbye", "end" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-invalid",
                Name = "Invalid Input",
                Description = "Played when caller enters invalid input",
                Type = PromptType.Tts,
                TtsText = "I'm sorry, that is not a valid option. Please try again. If this is an emergency, press 1.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "System",
                Tags = new List<string> { "error", "invalid" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-timeout",
                Name = "Timeout",
                Description = "Played when no input is received",
                Type = PromptType.Tts,
                TtsText = "We didn't receive any input. Please make a selection. Press 1 for emergencies or 0 for an operator.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "System",
                Tags = new List<string> { "timeout" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "prompt-transfer-vdn",
                Name = "VDN Transfer",
                Description = "Played before transferring to CM10 VDN",
                Type = PromptType.Tts,
                TtsText = "Please hold while we transfer your call.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Transfer",
                Tags = new List<string> { "transfer", "vdn", "cm10" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = "prompt-speech",
                Name = "Speech Routing",
                Description = "AI speech routing for E911 admin",
                Type = PromptType.Tts,
                TtsText = "Please briefly describe the reason for your call — for example, I need a fire dispatcher, or I need an alarm administrator.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Speech",
                Tags = new List<string> { "speech", "ai", "routing" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        foreach (var p in prompts)
            _prompts[p.Id] = p;
    }

    private void SeedMenus()
    {
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
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-alarm-submitted" } },
                new() { DtmfKey = "2", Label = "Test",
                    SpeechKeywords = new List<string> { "test", "testing" },
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-alarm-submitted" } },
                new() { DtmfKey = "3", Label = "Maintenance",
                    SpeechKeywords = new List<string> { "maintenance", "repair", "service" },
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-alarm-submitted" } }
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

        foreach (var m in new IvrMenu[]
        {
            mainMenu, alarmAdminMenu, alarmSelectMenu, alarmStatusMenu,
            fireDispatchMenu, fireReasonMenu,
            policeDispatchMenu, policeReasonMenu,
            afterHoursMenu, speechMenu
        })
            _menus[m.Id] = m;
    }

    private void SeedAniRecords()
    {
        var records = new List<AniRecord>
        {
            new()
            {
                Id = "ani-1",
                PhoneNumber = "+18085551234",
                CallerName = "JBPHH — Bldg 1 (HQ Pacific Fleet)",
                AccountNumber = "NAV-10042",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 25,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new()
            {
                Id = "ani-2",
                PhoneNumber = "+18085559876",
                CallerName = "Hickam — Bldg 1100 (15th Wing HQ)",
                AccountNumber = "NAV-20015",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 20,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new()
            {
                Id = "ani-3",
                PhoneNumber = "+18085550001",
                CallerName = "Spam / Telemarketer",
                CallerType = CallerType.Unknown,
                Language = "en-US",
                IsBlocked = true,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            },
            new()
            {
                Id = "ani-4",
                PhoneNumber = "+18085552223",
                CallerName = "West Loch — Magazine Area Operations",
                AccountNumber = "NAV-30078",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "ani-5",
                PhoneNumber = "+18085554445",
                CallerName = "NCTAMS PAC — Wahiawa Comm Station",
                AccountNumber = "NAV-5001",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 25,
                CreatedAt = DateTime.UtcNow.AddDays(-120)
            },
            new()
            {
                Id = "ani-6",
                PhoneNumber = "+18085556667",
                CallerName = "PMRF — Barking Sands (Bldg 110)",
                AccountNumber = "NAV-40022",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = "ani-7",
                PhoneNumber = "+18085558889",
                CallerName = "Tripler Army Medical Center",
                AccountNumber = "NAV-50033",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 20,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new()
            {
                Id = "ani-8",
                PhoneNumber = "+18085553334",
                CallerName = "JBPHH — Makalapa Housing",
                AccountNumber = "NAV-60011",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new()
            {
                Id = "ani-9",
                PhoneNumber = "+18085557778",
                CallerName = "RDC — Remote Dispatch Center",
                AccountNumber = "RDC-001",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 30,
                CreatedAt = DateTime.UtcNow.AddDays(-180)
            }
        };

        foreach (var r in records)
            _aniRecords[r.Id] = r;
    }

    private void SeedAliRecords()
    {
        var records = new List<AliRecord>
        {
            new()
            {
                Id = "ali-1",
                PhoneNumber = "+18085551234",
                Address = new Address { Street = "Bldg 1, Pacific Fleet HQ", City = "Joint Base Pearl Harbor-Hickam", State = "HI", ZipCode = "96860" },
                Coordinates = new GeoCoordinates { Latitude = 21.3547, Longitude = -157.9500 },
                LocationType = LocationType.Commercial,
                ServiceArea = "JBPHH",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            },
            new()
            {
                Id = "ali-2",
                PhoneNumber = "+18085559876",
                Address = new Address { Street = "Bldg 1100, 15th Wing HQ", City = "Hickam Field", State = "HI", ZipCode = "96853" },
                Coordinates = new GeoCoordinates { Latitude = 21.3187, Longitude = -157.9246 },
                LocationType = LocationType.Commercial,
                ServiceArea = "Hickam",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            },
            new()
            {
                Id = "ali-3",
                PhoneNumber = "+18085552223",
                Address = new Address { Street = "West Loch Annex, Magazine Rd", City = "Ewa Beach", State = "HI", ZipCode = "96706" },
                Coordinates = new GeoCoordinates { Latitude = 21.3400, Longitude = -157.9800 },
                LocationType = LocationType.Commercial,
                ServiceArea = "West Loch",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            },
            new()
            {
                Id = "ali-4",
                PhoneNumber = "+18085554445",
                Address = new Address { Street = "NCTAMS PAC, Wahiawa Station", City = "Wahiawa", State = "HI", ZipCode = "96786" },
                Coordinates = new GeoCoordinates { Latitude = 21.5000, Longitude = -158.0236 },
                LocationType = LocationType.Commercial,
                ServiceArea = "NCTAMS",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            },
            new()
            {
                Id = "ali-5",
                PhoneNumber = "+18085556667",
                Address = new Address { Street = "Bldg 110, Barking Sands", City = "Kekaha", State = "HI", ZipCode = "96752" },
                Coordinates = new GeoCoordinates { Latitude = 22.0226, Longitude = -159.7850 },
                LocationType = LocationType.Commercial,
                ServiceArea = "PMRF",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            },
            new()
            {
                Id = "ali-6",
                PhoneNumber = "+18085558889",
                Address = new Address { Street = "1 Jarrett White Rd", City = "Tripler AMC", State = "HI", ZipCode = "96859" },
                Coordinates = new GeoCoordinates { Latitude = 21.3625, Longitude = -157.8861 },
                LocationType = LocationType.Commercial,
                ServiceArea = "JBPHH",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            },
            new()
            {
                Id = "ali-7",
                PhoneNumber = "+18085553334",
                Address = new Address { Street = "Makalapa Housing Area", City = "Joint Base Pearl Harbor-Hickam", State = "HI", ZipCode = "96860" },
                Coordinates = new GeoCoordinates { Latitude = 21.3680, Longitude = -157.9420 },
                LocationType = LocationType.Residential,
                ServiceArea = "JBPHH",
                Region = "Hawaii",
                Timezone = "Pacific/Honolulu"
            }
        };

        foreach (var r in records)
            _aliRecords[r.Id] = r;
    }

    private void SeedCallLogs()
    {
        var rng = new Random(42);
        var callers = new[] { "+18085551234", "+18085559876", "+18085552223", "+18085556667", "+18085558889", "+18085553334", "+18085557778" };
        var callerNames = new[] { "JBPHH HQ", "Hickam 15th Wing", "West Loch Ops", "PMRF Barking Sands", "Tripler AMC", "Makalapa Housing", "RDC Callback" };
        var dids = new[] { "+18085551000", "+18085551001" };
        var dispositions = new[] {
            CallDisposition.Completed,
            CallDisposition.TransferredToAgent,
            CallDisposition.TransferredToQueue,
            CallDisposition.CallerHangup,
            CallDisposition.TransferredToVdn
        };
        var teams = new[] { "RDC Emergency", "Alarm Administrator", "Fire Dispatch", "Police Dispatch", null };
        var menuPaths = new[] {
            new[] { ("menu-main", "1") },          // Emergency → RDC
            new[] { ("menu-main", "2"), ("menu-alarm-admin", "1") },  // Alarm Admin → JBPHH
            new[] { ("menu-main", "2"), ("menu-alarm-admin", "3") },  // Alarm Admin → West Loch
            new[] { ("menu-main", "3"), ("menu-fire-dispatch", "1") }, // Fire → Fed Fire
            new[] { ("menu-main", "4"), ("menu-police-dispatch", "1") }, // Police → JB Police
        };

        for (int i = 0; i < 30; i++)
        {
            var callerIdx = rng.Next(callers.Length);
            var startTime = DateTime.UtcNow.AddHours(-rng.Next(1, 168));
            var duration = rng.Next(30, 480);
            var disposition = dispositions[rng.Next(dispositions.Length)];
            var pathIdx = rng.Next(menuPaths.Length);

            var log = new CallLog
            {
                Id = $"call-{i + 1:D3}",
                CallId = Guid.NewGuid().ToString(),
                CallerNumber = callers[callerIdx],
                CalledNumber = dids[rng.Next(dids.Length)],
                StartTime = startTime,
                EndTime = startTime.AddSeconds(duration),
                DurationSeconds = duration,
                Status = CallStatus.Completed,
                Disposition = disposition,
                MenuPath = menuPaths[pathIdx].Select((step, idx) => new MenuPathEntry
                {
                    MenuId = step.Item1,
                    MenuName = step.Item1.Replace("menu-", "").Replace("-", " "),
                    Input = step.Item2,
                    Timestamp = startTime.AddSeconds(5 + idx * 8)
                }).ToList(),
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = "seed-data",
                    ["accountNumber"] = $"FA-{rng.Next(10000, 99999)}"
                }
            };

            if (disposition == CallDisposition.TransferredToAgent || disposition == CallDisposition.TransferredToQueue)
            {
                log.RoutedToTeam = teams[rng.Next(teams.Length)];
                log.QueueName = log.RoutedToTeam?.ToLower().Replace(" ", "-") + "-queue";
            }

            if (disposition == CallDisposition.TransferredToVdn)
            {
                // Emergency transfers go to RDC VDN
                log.TransferredTo = "sip:70200@sbc.yourdomain.com";
                log.RoutedToTeam = "RDC Emergency";
            }

            _callLogs[log.Id] = log;
        }
    }

    private void SeedTeamRouting()
    {
        var teams = new List<TeamRoutingConfig>
        {
            new()
            {
                Id = "team-rdc",
                TeamName = "RDC Emergency",
                Description = "Remote Dispatch Center — handles all emergency and life-safety calls. Transfers immediately.",
                TransferNumber = "+18005559911",
                QueueName = "rdc-emergency",
                IntentKeywords = new List<string> { "emergency", "fire", "alarm", "smoke", "dispatch", "life safety", "urgent", "911", "rdc" },
                Priority = 100,
                IsActive = true
            },
            new()
            {
                Id = "team-alarm-admin",
                TeamName = "Alarm Administrator",
                Description = "Handles alarm administration for LAMAS areas — Pearl Harbor, Hickam, West Loch, NCTAMS, PMRF",
                TransferNumber = "+18005551001",
                QueueName = "alarm-admin-queue",
                IntentKeywords = new List<string> { "alarm", "administrator", "lamas", "monitoring", "panel", "sensor", "alarm admin" },
                Priority = 10,
                IsActive = true
            },
            new()
            {
                Id = "team-fire-dispatch",
                TeamName = "Fire Dispatch",
                Description = "Fire dispatcher — Fed Fire and PMRF fire departments",
                TransferNumber = "+18005551002",
                QueueName = "fire-dispatch-queue",
                IntentKeywords = new List<string> { "fire", "fire dispatcher", "fed fire", "fire department", "fire truck", "blaze" },
                Priority = 15,
                IsActive = true
            },
            new()
            {
                Id = "team-police-dispatch",
                TeamName = "Police Dispatch",
                Description = "Police dispatcher — Joint Base Police and PMRF police departments",
                TransferNumber = "+18005551003",
                QueueName = "police-dispatch-queue",
                IntentKeywords = new List<string> { "police", "police dispatcher", "joint base police", "law enforcement", "security", "officer" },
                Priority = 5,
                IsActive = true
            }
        };

        foreach (var t in teams)
            _teamRouting[t.Id] = t;
    }

    private void SeedPhoneNumbers()
    {
        var numbers = new List<PhoneNumberConfig>
        {
            new()
            {
                Id = "pn-main",
                PhoneNumber = "+18005551000",
                Label = "E911 Admin Line — Main",
                NumberType = PstnNumberType.DirectRouting,
                RootMenuId = "menu-main",
                IsActive = true
            },
            new()
            {
                Id = "pn-inspection",
                PhoneNumber = "+18005551001",
                Label = "Inspection Scheduling Direct",
                NumberType = PstnNumberType.DirectRouting,
                RootMenuId = "menu-inspection",
                IsActive = true
            },
            new()
            {
                Id = "pn-maintenance",
                PhoneNumber = "+18005551002",
                Label = "Maintenance & Repair Hotline",
                NumberType = PstnNumberType.DirectRouting,
                RootMenuId = "menu-maintenance",
                IsActive = true
            },
            new()
            {
                Id = "pn-rdc",
                PhoneNumber = "+18005559911",
                Label = "RDC Emergency Transfer",
                NumberType = PstnNumberType.DirectRouting,
                RootMenuId = "menu-main",
                IsActive = true
            }
        };

        foreach (var n in numbers)
            _phoneNumbers[n.Id] = n;
    }

    private void SeedBusinessHours()
    {
        var config = new BusinessHoursConfig
        {
            Id = "bh-default",
            Name = "E911 Admin Business Hours",
            Timezone = "America/Chicago",
            AfterHoursMenuId = "menu-afterhours",
            IsActive = true,
            Schedule = new Dictionary<DayOfWeek, DaySchedule>
            {
                [DayOfWeek.Monday]    = new() { IsOpen = true, OpenTime = "07:00", CloseTime = "18:00" },
                [DayOfWeek.Tuesday]   = new() { IsOpen = true, OpenTime = "07:00", CloseTime = "18:00" },
                [DayOfWeek.Wednesday] = new() { IsOpen = true, OpenTime = "07:00", CloseTime = "18:00" },
                [DayOfWeek.Thursday]  = new() { IsOpen = true, OpenTime = "07:00", CloseTime = "18:00" },
                [DayOfWeek.Friday]    = new() { IsOpen = true, OpenTime = "07:00", CloseTime = "18:00" },
                [DayOfWeek.Saturday]  = new() { IsOpen = false },
                [DayOfWeek.Sunday]    = new() { IsOpen = false }
            },
            Holidays = new List<Holiday>
            {
                new() { Date = new DateTime(2026, 1, 1), Name = "New Year's Day" },
                new() { Date = new DateTime(2026, 5, 25), Name = "Memorial Day" },
                new() { Date = new DateTime(2026, 7, 4), Name = "Independence Day" },
                new() { Date = new DateTime(2026, 9, 7), Name = "Labor Day" },
                new() { Date = new DateTime(2026, 11, 26), Name = "Thanksgiving" },
                new() { Date = new DateTime(2026, 12, 25), Name = "Christmas Day" }
            }
        };

        _businessHours[config.Id] = config;
    }

    private void SeedSystemConfig()
    {
        _systemConfig = new SystemConfig
        {
            Id = "system-config",
            DefaultLanguage = "en-US",
            DefaultTtsVoice = "en-US-JennyNeural",
            MaxCallDurationMinutes = 60,
            EnableSpeechRecognition = true,
            RootMenuId = "menu-main",
            GlobalTimeoutSeconds = 10,
            GlobalMaxRetries = 3,
            PstnMode = PstnMode.DirectRouting,
            DefaultSbcFqdn = "sbc.yourdomain.com",
            DefaultSbcPort = 5061,
            EnableDisconnectTransferToVdn = true,
            DefaultCm10Vdn = "70100",
            Cm10SbcFqdn = "sbc.yourdomain.com",
            Cm10SbcPort = 5061,
            Cm10TransferPromptId = "prompt-emergency-transfer"
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  ICosmosDbService Implementation
    // ═══════════════════════════════════════════════════════════════

    // ─── ANI Records ─────────────────────────────────────────────

    public Task<AniRecord?> GetAniRecordAsync(string phoneNumber)
    {
        var record = _aniRecords.Values.FirstOrDefault(a => a.PhoneNumber == phoneNumber);
        return Task.FromResult(record);
    }

    public Task<List<AniRecord>> SearchAniRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null)
    {
        var query = _aniRecords.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(a => (a.PhoneNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (a.CallerName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (a.AccountNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        return Task.FromResult(query.Take(pageSize).ToList());
    }

    public Task<AniRecord> UpsertAniRecordAsync(AniRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;
        _aniRecords[record.Id] = record;
        return Task.FromResult(record);
    }

    public Task DeleteAniRecordAsync(string id, string partitionKey)
    {
        _aniRecords.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── ALI Records ─────────────────────────────────────────────

    public Task<AliRecord?> GetAliRecordAsync(string phoneNumber)
    {
        var record = _aliRecords.Values.FirstOrDefault(a => a.PhoneNumber == phoneNumber);
        return Task.FromResult(record);
    }

    public Task<List<AliRecord>> SearchAliRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null)
    {
        var query = _aliRecords.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(a => (a.PhoneNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (a.Address.City?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (a.Region?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        return Task.FromResult(query.Take(pageSize).ToList());
    }

    public Task<AliRecord> UpsertAliRecordAsync(AliRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;
        _aliRecords[record.Id] = record;
        return Task.FromResult(record);
    }

    public Task DeleteAliRecordAsync(string id, string partitionKey)
    {
        _aliRecords.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── IVR Menus ───────────────────────────────────────────────

    public Task<IvrMenu?> GetMenuAsync(string menuId)
    {
        _menus.TryGetValue(menuId, out var menu);
        return Task.FromResult(menu);
    }

    public Task<List<IvrMenu>> GetAllMenusAsync()
        => Task.FromResult(_menus.Values.OrderBy(m => m.Order).ToList());

    public Task<IvrMenu?> GetRootMenuAsync()
    {
        var root = _menus.Values.FirstOrDefault(m => m.IsRootMenu && m.IsActive);
        return Task.FromResult(root);
    }

    public Task<IvrMenu> UpsertMenuAsync(IvrMenu menu)
    {
        menu.UpdatedAt = DateTime.UtcNow;
        _menus[menu.Id] = menu;
        return Task.FromResult(menu);
    }

    public Task DeleteMenuAsync(string id)
    {
        _menus.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── Prompts ─────────────────────────────────────────────────

    public Task<IvrPrompt?> GetPromptAsync(string promptId)
    {
        _prompts.TryGetValue(promptId, out var prompt);
        return Task.FromResult(prompt);
    }

    public Task<List<IvrPrompt>> GetAllPromptsAsync()
        => Task.FromResult(_prompts.Values.OrderBy(p => p.Name).ToList());

    public Task<IvrPrompt> UpsertPromptAsync(IvrPrompt prompt)
    {
        prompt.UpdatedAt = DateTime.UtcNow;
        _prompts[prompt.Id] = prompt;
        return Task.FromResult(prompt);
    }

    public Task DeletePromptAsync(string id)
    {
        _prompts.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── Call Logs ───────────────────────────────────────────────

    public Task<CallLog> CreateCallLogAsync(CallLog log)
    {
        _callLogs[log.Id] = log;
        return Task.FromResult(log);
    }

    public Task<CallLog> UpdateCallLogAsync(CallLog log)
    {
        _callLogs[log.Id] = log;
        return Task.FromResult(log);
    }

    public Task<List<CallLog>> GetCallLogsAsync(DateTime? from = null, DateTime? to = null, int pageSize = 50)
    {
        var query = _callLogs.Values.AsEnumerable();
        if (from.HasValue)
            query = query.Where(c => c.StartTime >= from.Value);
        if (to.HasValue)
            query = query.Where(c => c.StartTime <= to.Value);
        return Task.FromResult(query.OrderByDescending(c => c.StartTime).Take(pageSize).ToList());
    }

    public Task<CallLog?> GetCallLogByCallIdAsync(string callId)
    {
        var log = _callLogs.Values.FirstOrDefault(c => c.CallId == callId);
        return Task.FromResult(log);
    }

    // ─── Business Hours ──────────────────────────────────────────

    public Task<BusinessHoursConfig?> GetBusinessHoursAsync(string? id = null)
    {
        if (id != null)
        {
            _businessHours.TryGetValue(id, out var bh);
            return Task.FromResult(bh);
        }
        return Task.FromResult(_businessHours.Values.FirstOrDefault(b => b.IsActive));
    }

    public Task<BusinessHoursConfig> UpsertBusinessHoursAsync(BusinessHoursConfig config)
    {
        _businessHours[config.Id] = config;
        return Task.FromResult(config);
    }

    // ─── System Config ───────────────────────────────────────────

    public Task<SystemConfig> GetSystemConfigAsync()
        => Task.FromResult(_systemConfig);

    public Task<SystemConfig> UpsertSystemConfigAsync(SystemConfig config)
    {
        _systemConfig = config;
        return Task.FromResult(config);
    }

    // ─── Team Routing ────────────────────────────────────────────

    public Task<TeamRoutingConfig?> GetTeamRoutingConfigAsync(string id)
    {
        _teamRouting.TryGetValue(id, out var config);
        return Task.FromResult(config);
    }

    public Task<List<TeamRoutingConfig>> GetTeamRoutingConfigsAsync(List<string>? ids = null)
    {
        if (ids != null)
            return Task.FromResult(_teamRouting.Values.Where(t => ids.Contains(t.Id)).ToList());
        return Task.FromResult(_teamRouting.Values.ToList());
    }

    public Task<List<TeamRoutingConfig>> GetAllActiveTeamRoutingConfigsAsync()
        => Task.FromResult(_teamRouting.Values.Where(t => t.IsActive).ToList());

    public Task<TeamRoutingConfig> UpsertTeamRoutingConfigAsync(TeamRoutingConfig config)
    {
        _teamRouting[config.Id] = config;
        return Task.FromResult(config);
    }

    public Task DeleteTeamRoutingConfigAsync(string id)
    {
        _teamRouting.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── External Systems ────────────────────────────────────────

    public Task<ExternalSystemConfig?> GetExternalSystemConfigAsync(string id)
    {
        _externalSystems.TryGetValue(id, out var config);
        return Task.FromResult(config);
    }

    public Task<List<ExternalSystemConfig>> GetAllActiveExternalSystemConfigsAsync()
        => Task.FromResult(_externalSystems.Values.Where(e => e.IsActive).ToList());

    public Task<ExternalSystemConfig> UpsertExternalSystemConfigAsync(ExternalSystemConfig config)
    {
        _externalSystems[config.Id] = config;
        return Task.FromResult(config);
    }

    public Task DeleteExternalSystemConfigAsync(string id)
    {
        _externalSystems.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── Data Extraction ─────────────────────────────────────────

    public Task<DataExtractionConfig?> GetDataExtractionConfigAsync(string id)
    {
        _dataExtraction.TryGetValue(id, out var config);
        return Task.FromResult(config);
    }

    public Task<List<DataExtractionConfig>> GetAllActiveDataExtractionConfigsAsync()
        => Task.FromResult(_dataExtraction.Values.Where(d => d.IsActive).ToList());

    public Task<DataExtractionConfig> UpsertDataExtractionConfigAsync(DataExtractionConfig config)
    {
        _dataExtraction[config.Id] = config;
        return Task.FromResult(config);
    }

    public Task DeleteDataExtractionConfigAsync(string id)
    {
        _dataExtraction.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // ─── Phone Numbers ───────────────────────────────────────────

    public Task<PhoneNumberConfig?> GetPhoneNumberConfigAsync(string phoneNumber)
    {
        var config = _phoneNumbers.Values.FirstOrDefault(p => p.PhoneNumber == phoneNumber);
        return Task.FromResult(config);
    }

    public Task<PhoneNumberConfig?> GetPhoneNumberConfigByIdAsync(string id)
    {
        _phoneNumbers.TryGetValue(id, out var config);
        return Task.FromResult(config);
    }

    public Task<List<PhoneNumberConfig>> GetAllPhoneNumberConfigsAsync(bool activeOnly = true)
    {
        var query = _phoneNumbers.Values.AsEnumerable();
        if (activeOnly)
            query = query.Where(p => p.IsActive);
        return Task.FromResult(query.ToList());
    }

    public Task<PhoneNumberConfig> UpsertPhoneNumberConfigAsync(PhoneNumberConfig config)
    {
        _phoneNumbers[config.Id] = config;
        return Task.FromResult(config);
    }

    public Task DeletePhoneNumberConfigAsync(string id)
    {
        _phoneNumbers.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
