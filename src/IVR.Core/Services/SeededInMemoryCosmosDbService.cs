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
                Description = "Main E911 admin line welcome — fire alarm maintenance company",
                Type = PromptType.Tts,
                TtsText = "Thank you for calling the E 9 1 1 administration line. If this is an emergency, press 1 to be transferred to the Remote Dispatch Center immediately. For fire alarm inspection scheduling, press 2. For maintenance and repair status, press 3. For account and billing inquiries, press 4. To speak with an operator, press 0.",
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
                Id = "prompt-inspection",
                Name = "Inspection Scheduling Menu",
                Description = "Fire alarm inspection scheduling menu",
                Type = PromptType.Tts,
                TtsText = "For fire alarm inspection scheduling: To schedule a new inspection, press 1. To reschedule an existing appointment, press 2. To confirm an upcoming inspection, press 3. To return to the main menu, press 9.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "inspection", "scheduling", "fire-alarm" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-maintenance",
                Name = "Maintenance & Repair Menu",
                Description = "Fire alarm maintenance and repair status menu",
                Type = PromptType.Tts,
                TtsText = "For maintenance and repair: To report a fire alarm malfunction, press 1. To check on an open work order, press 2. For panel replacement or upgrade inquiries, press 3. To return to the main menu, press 9.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "maintenance", "repair", "fire-alarm" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new()
            {
                Id = "prompt-billing",
                Name = "Account & Billing Menu",
                Description = "Account and billing inquiries",
                Type = PromptType.Tts,
                TtsText = "For account and billing: To check your account balance, press 1. To make a payment, press 2. To request a copy of your inspection report, press 3. To return to the main menu, press 9.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Menu",
                Tags = new List<string> { "billing", "account" },
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
                TtsText = "Please briefly describe the reason for your call — for example, schedule an inspection, report a malfunction, or check on a work order.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Speech",
                Tags = new List<string> { "speech", "ai", "routing" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-alarm-malfunction",
                Name = "Alarm Malfunction Reported",
                Description = "Confirmation when a fire alarm malfunction is reported",
                Type = PromptType.Tts,
                TtsText = "Thank you. A fire alarm malfunction has been logged. A technician will be dispatched. If this is a life safety emergency, please hang up and dial 9 1 1.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Confirmation",
                Tags = new List<string> { "malfunction", "alarm", "workorder" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            // ─── Data-collection prompts for multi-step flows ───
            new()
            {
                Id = "prompt-identify-property",
                Name = "Identify Property",
                Description = "Asks caller to identify which property this is regarding",
                Type = PromptType.Tts,
                TtsText = "Which property is this regarding? For Oakwood Office Park, press 1. For Sunrise Senior Living, press 2. For Hilltop Apartments, press 3. For County 9 1 1 Center, press 4. For another property, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "DataCollection",
                Tags = new List<string> { "property", "building", "identify" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-inspection-type",
                Name = "Inspection Type",
                Description = "Asks caller what type of inspection they need",
                Type = PromptType.Tts,
                TtsText = "What type of inspection do you need? For an annual N F P A 72 inspection, press 1. For a semi-annual panel check, press 2. For a fire drill coordination, press 3. For a post-incident system evaluation, press 4.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "DataCollection",
                Tags = new List<string> { "inspection", "type", "nfpa" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-confirm-caller",
                Name = "Confirm Caller Identity",
                Description = "Asks caller to confirm or provide their name and role",
                Type = PromptType.Tts,
                TtsText = "Please confirm your role. If you are the building manager, press 1. If you are the fire safety officer, press 2. If you are a tenant or occupant, press 3. For all others, press 4.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "DataCollection",
                Tags = new List<string> { "caller", "identity", "role" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-transfer-scheduling",
                Name = "Submission Confirmation — Scheduling",
                Description = "Confirmation after submitting inspection data to external system",
                Type = PromptType.Tts,
                TtsText = "Thank you. Your inspection request has been submitted to our scheduling system with your property and inspection details. You will receive a confirmation call or email shortly. Goodbye.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Transfer",
                Tags = new List<string> { "transfer", "scheduling", "confirm" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-identify-property-maint",
                Name = "Identify Property (Maintenance)",
                Description = "Asks caller to identify property for maintenance request",
                Type = PromptType.Tts,
                TtsText = "To help us dispatch the right technician, which property is this regarding? For Oakwood Office Park, press 1. For Sunrise Senior Living, press 2. For Hilltop Apartments, press 3. For County 9 1 1 Center, press 4. For another property, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "DataCollection",
                Tags = new List<string> { "property", "maintenance" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-malfunction-type",
                Name = "Malfunction Type",
                Description = "Asks what kind of alarm malfunction",
                Type = PromptType.Tts,
                TtsText = "What is the nature of the issue? For a false alarm or nuisance alarm, press 1. For a panel fault or trouble signal, press 2. For a detector or pull station issue, press 3. For a notification appliance issue such as strobes or horns, press 4.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "DataCollection",
                Tags = new List<string> { "malfunction", "type", "alarm" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-transfer-maintenance",
                Name = "Submission Confirmation — Maintenance",
                Description = "Confirmation after submitting maintenance data to external system",
                Type = PromptType.Tts,
                TtsText = "Thank you for the details. Your work order has been submitted to our maintenance system. A technician will be dispatched and you will receive a confirmation shortly. Goodbye.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Transfer",
                Tags = new List<string> { "transfer", "maintenance", "workorder" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-account-lookup",
                Name = "Account Lookup",
                Description = "Asks for account number for billing",
                Type = PromptType.Tts,
                TtsText = "Which account is this regarding? For Oakwood Office Park, press 1. For Sunrise Senior Living, press 2. For Hilltop Apartments, press 3. For County 9 1 1 Center, press 4. For another account, press 5.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "DataCollection",
                Tags = new List<string> { "account", "billing", "lookup" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = "prompt-transfer-billing",
                Name = "Submission Confirmation — Billing",
                Description = "Confirmation after submitting billing inquiry to external system",
                Type = PromptType.Tts,
                TtsText = "Got it. Your billing inquiry has been submitted to our account system for that property. You will receive a follow-up with your account details. Goodbye.",
                TtsVoice = "en-US-JennyNeural",
                Language = "en-US",
                Category = "Transfer",
                Tags = new List<string> { "transfer", "billing" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        foreach (var p in prompts)
            _prompts[p.Id] = p;
    }

    private void SeedMenus()
    {
        var mainMenu = new IvrMenu
        {
            Id = "menu-main",
            Name = "E911 Admin Main Menu",
            Description = "Primary E911 administration line — fire alarm maintenance",
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
                    Label = "Emergency — Transfer to RDC",
                    SpeechKeywords = new List<string> { "emergency", "fire", "dispatch", "urgent", "life safety", "rdc" },
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
                    Label = "Inspection Scheduling",
                    SpeechKeywords = new List<string> { "inspection", "schedule", "annual", "test", "nfpa" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-inspection" }
                },
                new()
                {
                    DtmfKey = "3",
                    Label = "Maintenance & Repair",
                    SpeechKeywords = new List<string> { "maintenance", "repair", "malfunction", "broken", "trouble", "panel" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maintenance" }
                },
                new()
                {
                    DtmfKey = "4",
                    Label = "Account & Billing",
                    SpeechKeywords = new List<string> { "billing", "account", "payment", "invoice", "report" },
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-billing" }
                },
                new()
                {
                    DtmfKey = "0",
                    Label = "Operator",
                    SpeechKeywords = new List<string> { "operator", "agent", "person", "representative" },
                    Action = new MenuAction
                    {
                        Type = ActionType.TransferToVdn,
                        VdnAddress = "sip:70300@sbc.yourdomain.com",
                        PromptId = "prompt-hold"
                    }
                }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var inspectionMenu = new IvrMenu
        {
            Id = "menu-inspection",
            Name = "Inspection Scheduling",
            Description = "Fire alarm inspection scheduling — step 1: select service",
            PromptId = "prompt-inspection",
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
                new()
                {
                    DtmfKey = "1",
                    Label = "Schedule New Inspection",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-property" }
                },
                new()
                {
                    DtmfKey = "2",
                    Label = "Reschedule Existing",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-property" }
                },
                new()
                {
                    DtmfKey = "3",
                    Label = "Confirm Upcoming Inspection",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-property" }
                },
                new()
                {
                    DtmfKey = "9",
                    Label = "Main Menu",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-main" }
                }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Inspection: Step 2 — Which property? ───
        var inspPropertyMenu = new IvrMenu
        {
            Id = "menu-insp-property",
            Name = "Inspection — Property Selection",
            Description = "Select which property the inspection is for",
            PromptId = "prompt-identify-property",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-inspection",
            IsActive = true,
            TimeoutSeconds = 15,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Oakwood Office Park",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-type" } },
                new() { DtmfKey = "2", Label = "Sunrise Senior Living",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-type" } },
                new() { DtmfKey = "3", Label = "Hilltop Apartments",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-type" } },
                new() { DtmfKey = "4", Label = "County 911 Center",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-type" } },
                new() { DtmfKey = "5", Label = "Other Property",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-type" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // ─── Inspection: Step 3 — What type of inspection? ───
        var inspTypeMenu = new IvrMenu
        {
            Id = "menu-insp-type",
            Name = "Inspection — Type Selection",
            Description = "Select the type of inspection needed",
            PromptId = "prompt-inspection-type",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-insp-property",
            IsActive = true,
            TimeoutSeconds = 15,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 2,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Annual NFPA 72 Inspection",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-caller" } },
                new() { DtmfKey = "2", Label = "Semi-Annual Panel Check",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-caller" } },
                new() { DtmfKey = "3", Label = "Fire Drill Coordination",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-caller" } },
                new() { DtmfKey = "4", Label = "Post-Incident Evaluation",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-insp-caller" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // ─── Inspection: Step 4 — Who is calling? ───
        var inspCallerMenu = new IvrMenu
        {
            Id = "menu-insp-caller",
            Name = "Inspection — Caller Role",
            Description = "Confirm the caller's role at the property",
            PromptId = "prompt-confirm-caller",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-insp-type",
            IsActive = true,
            TimeoutSeconds = 15,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 3,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Building Manager",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-scheduling" } },
                new() { DtmfKey = "2", Label = "Fire Safety Officer",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-scheduling" } },
                new() { DtmfKey = "3", Label = "Tenant / Occupant",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-scheduling" } },
                new() { DtmfKey = "4", Label = "Other",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-scheduling" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var maintenanceMenu = new IvrMenu
        {
            Id = "menu-maintenance",
            Name = "Maintenance & Repair",
            Description = "Fire alarm maintenance — step 1: select service",
            PromptId = "prompt-maintenance",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-main",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            Order = 2,
            Options = new List<MenuOption>
            {
                new()
                {
                    DtmfKey = "1",
                    Label = "Report Alarm Malfunction",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-property" }
                },
                new()
                {
                    DtmfKey = "2",
                    Label = "Check Work Order Status",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-property" }
                },
                new()
                {
                    DtmfKey = "3",
                    Label = "Panel Replacement / Upgrade",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-property" }
                },
                new()
                {
                    DtmfKey = "9",
                    Label = "Main Menu",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-main" }
                }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        };

        // ─── Maintenance: Step 2 — Which property? ───
        var maintPropertyMenu = new IvrMenu
        {
            Id = "menu-maint-property",
            Name = "Maintenance — Property Selection",
            Description = "Select which property has the maintenance issue",
            PromptId = "prompt-identify-property-maint",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-maintenance",
            IsActive = true,
            TimeoutSeconds = 15,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Oakwood Office Park",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-issue" } },
                new() { DtmfKey = "2", Label = "Sunrise Senior Living",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-issue" } },
                new() { DtmfKey = "3", Label = "Hilltop Apartments",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-issue" } },
                new() { DtmfKey = "4", Label = "County 911 Center",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-issue" } },
                new() { DtmfKey = "5", Label = "Other Property",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-maint-issue" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // ─── Maintenance: Step 3 — What's the issue? ───
        var maintIssueMenu = new IvrMenu
        {
            Id = "menu-maint-issue",
            Name = "Maintenance — Issue Type",
            Description = "Select the type of maintenance issue",
            PromptId = "prompt-malfunction-type",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-maint-property",
            IsActive = true,
            TimeoutSeconds = 15,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 2,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "False / Nuisance Alarm",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-maintenance" } },
                new() { DtmfKey = "2", Label = "Panel Fault / Trouble Signal",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-maintenance" } },
                new() { DtmfKey = "3", Label = "Detector / Pull Station Issue",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-maintenance" } },
                new() { DtmfKey = "4", Label = "Notification Appliance (Strobes/Horns)",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-maintenance" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var billingMenu = new IvrMenu
        {
            Id = "menu-billing",
            Name = "Account & Billing",
            Description = "Billing — step 1: select service",
            PromptId = "prompt-billing",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-main",
            IsActive = true,
            TimeoutSeconds = 10,
            MaxRetries = 3,
            Order = 3,
            Options = new List<MenuOption>
            {
                new()
                {
                    DtmfKey = "1",
                    Label = "Check Balance",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-bill-account" }
                },
                new()
                {
                    DtmfKey = "2",
                    Label = "Make a Payment",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-bill-account" }
                },
                new()
                {
                    DtmfKey = "3",
                    Label = "Request Inspection Report",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-bill-account" }
                },
                new()
                {
                    DtmfKey = "9",
                    Label = "Main Menu",
                    Action = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-main" }
                }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-25)
        };

        // ─── Billing: Step 2 — Which account? ───
        var billAccountMenu = new IvrMenu
        {
            Id = "menu-bill-account",
            Name = "Billing — Account Selection",
            Description = "Select which account this billing inquiry is for",
            PromptId = "prompt-account-lookup",
            MenuType = MenuType.SubMenu,
            ParentMenuId = "menu-billing",
            IsActive = true,
            TimeoutSeconds = 15,
            MaxRetries = 3,
            TimeoutPromptId = "prompt-timeout",
            InvalidInputPromptId = "prompt-invalid",
            Order = 1,
            Options = new List<MenuOption>
            {
                new() { DtmfKey = "1", Label = "Oakwood Office Park",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-billing" } },
                new() { DtmfKey = "2", Label = "Sunrise Senior Living",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-billing" } },
                new() { DtmfKey = "3", Label = "Hilltop Apartments",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-billing" } },
                new() { DtmfKey = "4", Label = "County 911 Center",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-billing" } },
                new() { DtmfKey = "5", Label = "Other Account",
                    Action = new MenuAction { Type = ActionType.Webhook, WebhookUrl = "http://pstn-simulator:8080/api/external/work-order", PromptId = "prompt-transfer-billing" } }
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
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
            TeamRoutingConfigIds = new List<string> { "team-rdc", "team-inspection", "team-maintenance", "team-billing" },
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
            mainMenu, inspectionMenu, inspPropertyMenu, inspTypeMenu, inspCallerMenu,
            maintenanceMenu, maintPropertyMenu, maintIssueMenu,
            billingMenu, billAccountMenu,
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
                PhoneNumber = "+15551234567",
                CallerName = "Oakwood Office Park — Bldg Mgmt",
                AccountNumber = "FA-10042",
                CallerType = CallerType.Business,
                Language = "en-US",
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new()
            {
                Id = "ani-2",
                PhoneNumber = "+15559876543",
                CallerName = "City of Springfield — Fire Marshal",
                AccountNumber = "GOV-20015",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 20,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new()
            {
                Id = "ani-3",
                PhoneNumber = "+15550001111",
                CallerName = "Spam / Telemarketer",
                CallerType = CallerType.Unknown,
                Language = "en-US",
                IsBlocked = true,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            },
            new()
            {
                Id = "ani-4",
                PhoneNumber = "+15552223333",
                CallerName = "Sunrise Senior Living — Facilities",
                AccountNumber = "FA-30078",
                CallerType = CallerType.Business,
                Language = "en-US",
                IsVip = true,
                Priority = 15,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new()
            {
                Id = "ani-5",
                PhoneNumber = "+15554445555",
                CallerName = "County 911 Center — Admin",
                AccountNumber = "GOV-5001",
                CallerType = CallerType.Government,
                Language = "en-US",
                IsVip = true,
                Priority = 25,
                CreatedAt = DateTime.UtcNow.AddDays(-120)
            },
            new()
            {
                Id = "ani-6",
                PhoneNumber = "+15556667777",
                CallerName = "Hilltop Apartments — Super",
                AccountNumber = "FA-40022",
                CallerType = CallerType.Residential,
                Language = "en-US",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new()
            {
                Id = "ani-7",
                PhoneNumber = "+15558889999",
                CallerName = "Regional Hospital — Safety Dept",
                AccountNumber = "FA-50033",
                CallerType = CallerType.Business,
                Language = "en-US",
                IsVip = true,
                Priority = 20,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new()
            {
                Id = "ani-8",
                PhoneNumber = "+15553334444",
                CallerName = "Lincoln Elementary School",
                AccountNumber = "FA-60011",
                CallerType = CallerType.Business,
                Language = "en-US",
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new()
            {
                Id = "ani-9",
                PhoneNumber = "+15557778888",
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
                PhoneNumber = "+15551234567",
                Address = new Address { Street = "200 Oakwood Blvd, Suite 100", City = "Springfield", State = "IL", ZipCode = "62701" },
                Coordinates = new GeoCoordinates { Latitude = 39.7817, Longitude = -89.6501 },
                LocationType = LocationType.Commercial,
                ServiceArea = "Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            },
            new()
            {
                Id = "ali-2",
                PhoneNumber = "+15559876543",
                Address = new Address { Street = "1 Government Plaza", City = "Springfield", State = "IL", ZipCode = "62702" },
                Coordinates = new GeoCoordinates { Latitude = 39.7990, Longitude = -89.6440 },
                LocationType = LocationType.Commercial,
                ServiceArea = "Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            },
            new()
            {
                Id = "ali-3",
                PhoneNumber = "+15552223333",
                Address = new Address { Street = "450 Sunrise Drive", City = "Decatur", State = "IL", ZipCode = "62521" },
                Coordinates = new GeoCoordinates { Latitude = 39.8403, Longitude = -88.9548 },
                LocationType = LocationType.Commercial,
                ServiceArea = "Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            },
            new()
            {
                Id = "ali-4",
                PhoneNumber = "+15554445555",
                Address = new Address { Street = "800 County Center Dr", City = "Champaign", State = "IL", ZipCode = "61820" },
                Coordinates = new GeoCoordinates { Latitude = 40.1164, Longitude = -88.2434 },
                LocationType = LocationType.Commercial,
                ServiceArea = "East Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            },
            new()
            {
                Id = "ali-5",
                PhoneNumber = "+15556667777",
                Address = new Address { Street = "1200 Hilltop Lane", City = "Bloomington", State = "IL", ZipCode = "61701" },
                Coordinates = new GeoCoordinates { Latitude = 40.4842, Longitude = -88.9937 },
                LocationType = LocationType.Residential,
                ServiceArea = "Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            },
            new()
            {
                Id = "ali-6",
                PhoneNumber = "+15558889999",
                Address = new Address { Street = "500 Medical Center Pkwy", City = "Peoria", State = "IL", ZipCode = "61602" },
                Coordinates = new GeoCoordinates { Latitude = 40.6936, Longitude = -89.5890 },
                LocationType = LocationType.Commercial,
                ServiceArea = "Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            },
            new()
            {
                Id = "ali-7",
                PhoneNumber = "+15553334444",
                Address = new Address { Street = "300 Lincoln Ave", City = "Springfield", State = "IL", ZipCode = "62704" },
                Coordinates = new GeoCoordinates { Latitude = 39.7717, Longitude = -89.6601 },
                LocationType = LocationType.Commercial,
                ServiceArea = "Central Illinois",
                Region = "Midwest",
                Timezone = "America/Chicago"
            }
        };

        foreach (var r in records)
            _aliRecords[r.Id] = r;
    }

    private void SeedCallLogs()
    {
        var rng = new Random(42);
        var callers = new[] { "+15551234567", "+15559876543", "+15552223333", "+15556667777", "+15558889999", "+15553334444", "+15557778888" };
        var callerNames = new[] { "Oakwood Office Park", "City Fire Marshal", "Sunrise Senior Living", "Hilltop Apartments", "Regional Hospital", "Lincoln Elementary", "RDC Callback" };
        var dids = new[] { "+18005551000", "+18005551001" };
        var dispositions = new[] {
            CallDisposition.Completed,
            CallDisposition.TransferredToAgent,
            CallDisposition.TransferredToQueue,
            CallDisposition.CallerHangup,
            CallDisposition.TransferredToVdn
        };
        var teams = new[] { "RDC Emergency", "Inspection Scheduling", "Maintenance", "Billing", null };
        var menuPaths = new[] {
            new[] { ("menu-main", "1") },          // Emergency → RDC
            new[] { ("menu-main", "2"), ("menu-inspection", "1") },  // Schedule inspection
            new[] { ("menu-main", "3"), ("menu-maintenance", "1") }, // Report malfunction
            new[] { ("menu-main", "3"), ("menu-maintenance", "2") }, // Check work order
            new[] { ("menu-main", "4"), ("menu-billing", "1") },     // Check balance
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
                Id = "team-inspection",
                TeamName = "Inspection Scheduling",
                Description = "Handles fire alarm inspection scheduling — annual, semi-annual, NFPA compliance inspections",
                TransferNumber = "+18005551001",
                QueueName = "inspection-scheduling",
                IntentKeywords = new List<string> { "inspection", "schedule", "annual", "test", "nfpa", "compliance", "certify", "appointment" },
                Priority = 10,
                IsActive = true
            },
            new()
            {
                Id = "team-maintenance",
                TeamName = "Maintenance & Repair",
                Description = "Handles fire alarm system maintenance, repair, panel replacement, malfunction reports, and work orders",
                TransferNumber = "+18005551002",
                QueueName = "maintenance-queue",
                IntentKeywords = new List<string> { "malfunction", "repair", "broken", "panel", "trouble", "beeping", "fault", "work order", "replace", "upgrade" },
                Priority = 15,
                IsActive = true
            },
            new()
            {
                Id = "team-billing",
                TeamName = "Account & Billing",
                Description = "Handles account balances, payments, inspection reports, and invoicing",
                TransferNumber = "+18005551003",
                QueueName = "billing-queue",
                IntentKeywords = new List<string> { "billing", "payment", "invoice", "balance", "report", "certificate", "account" },
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
