using IVR.Core.Models;

namespace IVR.DataSeeder;

/// <summary>
/// Generates test team routing configs for E911 IVR system.
/// </summary>
public static class TeamRoutingSeeder
{
    public static List<TeamRoutingConfig> GenerateTestTeamRoutingConfigs()
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

        return teams;
    }
}
