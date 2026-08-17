using IVR.Core.Models;

namespace IVR.DataSeeder;

/// <summary>
/// Generates the ExternalSystemConfig/DataExtractionConfig pair from the fire-alarm
/// example in docs/system-integration.md, targeting the PSTN simulator's mock
/// "/api/external/work-order" endpoint so the full SubmitToExternalSystem pipeline
/// (AI extraction → HTTP submit → success/failure prompt → post-submit action) is testable.
/// </summary>
public static class ExternalSystemSeeder
{
    /// <param name="webhookBaseUrl">Same base URL passed to <see cref="MenuSeeder.GenerateTestMenus"/>.</param>
    public static List<ExternalSystemConfig> GenerateTestExternalSystems(string webhookBaseUrl = "http://pstn-simulator:8080")
    {
        return new List<ExternalSystemConfig>
        {
            new ExternalSystemConfig
            {
                Id = "sys-fire-alarm-001",
                SystemName = "Fire Alarm Panel",
                Description = "Building fire alarm control system (routed to the PSTN simulator's mock endpoint for testing)",
                SystemType = ExternalSystemType.RestApi,
                BaseUrl = $"{webhookBaseUrl.TrimEnd('/')}/api/external",
                Endpoints = new List<ExternalSystemEndpoint>
                {
                    new ExternalSystemEndpoint
                    {
                        ActionName = "put-in-test",
                        Description = "Put fire alarm into test mode",
                        HttpMethod = "POST",
                        UrlPath = "work-order",
                        PayloadTemplate = "{ \"action\": \"{{action}}\", \"building\": \"{{location}}\", \"requestedBy\": \"{{callerNumber}}\" }",
                        ResponseConfirmationField = "workOrderId"
                    }
                },
                AuthType = ExternalAuthType.None,
                RetryPolicy = new RetryPolicy { MaxRetries = 2, InitialDelayMs = 1000, BackoffMultiplier = 2.0 },
                TimeoutSeconds = 15,
                IsActive = true
            }
        };
    }

    public static List<DataExtractionConfig> GenerateTestDataExtractionConfigs()
    {
        return new List<DataExtractionConfig>
        {
            new DataExtractionConfig
            {
                Id = "extract-fire-alarm-001",
                Name = "Fire Alarm Actions",
                Description = "Extract fire alarm action details from caller transcript",
                Fields = new List<ExtractionField>
                {
                    new ExtractionField
                    {
                        FieldName = "action",
                        Label = "Action",
                        Description = "The action the caller wants to perform on the fire alarm system",
                        DataType = ExtractionFieldType.Enum,
                        Required = true,
                        ValidValues = new List<string> { "test", "reset", "silence", "acknowledge" },
                        ValueAliases = new Dictionary<string, string>
                        {
                            { "put in test", "test" },
                            { "testing", "test" },
                            { "quiet", "silence" },
                            { "shut off", "silence" }
                        }
                    },
                    new ExtractionField
                    {
                        FieldName = "location",
                        Label = "Location",
                        Description = "The building or floor where the fire alarm is located",
                        DataType = ExtractionFieldType.String,
                        Required = true
                    },
                    new ExtractionField
                    {
                        FieldName = "systemType",
                        Label = "System Type",
                        Description = "Type of alarm system",
                        DataType = ExtractionFieldType.Enum,
                        Required = false,
                        DefaultValue = "fire_alarm",
                        ValidValues = new List<string> { "fire_alarm", "sprinkler", "smoke_detector" }
                    }
                },
                ExternalSystemId = "sys-fire-alarm-001",
                EndpointActionName = "put-in-test",
                AiContextInstructions = "The caller is reporting a fire alarm system action. Locations are typically building numbers or floor names within a campus.",
                RequireCallerConfirmation = false,
                // No SuccessPromptId set — this demonstrates the dynamic SuccessTtsTemplate path
                // (SuccessPromptId, if set, would take priority and skip the {{placeholder}} rendering).
                SuccessTtsTemplate = "Done. Fire alarms at {{location}} have been placed in {{action}} mode. Your reference number is {{confirmationValue}}.",
                FailurePromptId = "prompt-alarm-error",
                PostSubmitAction = new MenuAction { Type = ActionType.Hangup, PromptId = "prompt-goodbye" },
                PostFailureAction = new MenuAction { Type = ActionType.NavigateToMenu, TargetMenuId = "menu-fire-dispatch" },
                IsActive = true
            }
        };
    }
}
