using IVR.Core.Models;

namespace IVR.Core.Interfaces;

public interface ICosmosDbService
{
    // ANI Records
    Task<AniRecord?> GetAniRecordAsync(string phoneNumber);
    Task<List<AniRecord>> SearchAniRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null);
    Task<AniRecord> UpsertAniRecordAsync(AniRecord record);
    Task DeleteAniRecordAsync(string id, string partitionKey);

    // ALI Records
    Task<AliRecord?> GetAliRecordAsync(string phoneNumber);
    Task<List<AliRecord>> SearchAliRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null);
    Task<AliRecord> UpsertAliRecordAsync(AliRecord record);
    Task DeleteAliRecordAsync(string id, string partitionKey);

    // IVR Menus
    Task<IvrMenu?> GetMenuAsync(string menuId);
    Task<List<IvrMenu>> GetAllMenusAsync();
    Task<IvrMenu?> GetRootMenuAsync();
    Task<IvrMenu> UpsertMenuAsync(IvrMenu menu);
    Task DeleteMenuAsync(string id);

    // Prompts
    Task<IvrPrompt?> GetPromptAsync(string promptId);
    Task<List<IvrPrompt>> GetAllPromptsAsync();
    Task<IvrPrompt> UpsertPromptAsync(IvrPrompt prompt);
    Task DeletePromptAsync(string id);

    // Call Logs
    Task<CallLog> CreateCallLogAsync(CallLog log);
    Task<CallLog> UpdateCallLogAsync(CallLog log);
    Task<List<CallLog>> GetCallLogsAsync(DateTime? from = null, DateTime? to = null, int pageSize = 50);
    Task<CallLog?> GetCallLogByCallIdAsync(string callId);

    // Business Hours
    Task<BusinessHoursConfig?> GetBusinessHoursAsync(string? id = null);
    Task<BusinessHoursConfig> UpsertBusinessHoursAsync(BusinessHoursConfig config);

    // System Config
    Task<SystemConfig> GetSystemConfigAsync();
    Task<SystemConfig> UpsertSystemConfigAsync(SystemConfig config);

    // Team Routing
    Task<TeamRoutingConfig?> GetTeamRoutingConfigAsync(string id);
    Task<List<TeamRoutingConfig>> GetTeamRoutingConfigsAsync(List<string>? ids = null);
    Task<List<TeamRoutingConfig>> GetAllActiveTeamRoutingConfigsAsync();
    Task<TeamRoutingConfig> UpsertTeamRoutingConfigAsync(TeamRoutingConfig config);
    Task DeleteTeamRoutingConfigAsync(string id);

    // External Systems
    Task<ExternalSystemConfig?> GetExternalSystemConfigAsync(string id);
    Task<List<ExternalSystemConfig>> GetAllActiveExternalSystemConfigsAsync();
    Task<ExternalSystemConfig> UpsertExternalSystemConfigAsync(ExternalSystemConfig config);
    Task DeleteExternalSystemConfigAsync(string id);

    // Data Extraction
    Task<DataExtractionConfig?> GetDataExtractionConfigAsync(string id);
    Task<List<DataExtractionConfig>> GetAllActiveDataExtractionConfigsAsync();
    Task<DataExtractionConfig> UpsertDataExtractionConfigAsync(DataExtractionConfig config);
    Task DeleteDataExtractionConfigAsync(string id);

    // Phone Number Config (PSTN / Direct Routing)
    Task<PhoneNumberConfig?> GetPhoneNumberConfigAsync(string phoneNumber);
    Task<PhoneNumberConfig?> GetPhoneNumberConfigByIdAsync(string id);
    Task<List<PhoneNumberConfig>> GetAllPhoneNumberConfigsAsync(bool activeOnly = true);
    Task<PhoneNumberConfig> UpsertPhoneNumberConfigAsync(PhoneNumberConfig config);
    Task DeletePhoneNumberConfigAsync(string id);
}
