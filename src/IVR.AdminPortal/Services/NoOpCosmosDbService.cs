using IVR.Core.Interfaces;
using IVR.Core.Models;

namespace IVR.AdminPortal.Services;

/// <summary>
/// No-op implementation of ICosmosDbService used when Cosmos DB credentials are not configured.
/// Returns empty collections and default values for all operations.
/// </summary>
public class NoOpCosmosDbService : ICosmosDbService
{
    public Task<AniRecord?> GetAniRecordAsync(string phoneNumber) => Task.FromResult<AniRecord?>(null);
    public Task<List<AniRecord>> SearchAniRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null) => Task.FromResult(new List<AniRecord>());
    public Task<AniRecord> UpsertAniRecordAsync(AniRecord record) => Task.FromResult(record);
    public Task DeleteAniRecordAsync(string id, string partitionKey) => Task.CompletedTask;

    public Task<AliRecord?> GetAliRecordAsync(string phoneNumber) => Task.FromResult<AliRecord?>(null);
    public Task<List<AliRecord>> SearchAliRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null) => Task.FromResult(new List<AliRecord>());
    public Task<AliRecord> UpsertAliRecordAsync(AliRecord record) => Task.FromResult(record);
    public Task DeleteAliRecordAsync(string id, string partitionKey) => Task.CompletedTask;

    public Task<IvrMenu?> GetMenuAsync(string menuId) => Task.FromResult<IvrMenu?>(null);
    public Task<List<IvrMenu>> GetAllMenusAsync() => Task.FromResult(new List<IvrMenu>());
    public Task<IvrMenu?> GetRootMenuAsync() => Task.FromResult<IvrMenu?>(null);
    public Task<IvrMenu> UpsertMenuAsync(IvrMenu menu) => Task.FromResult(menu);
    public Task DeleteMenuAsync(string id) => Task.CompletedTask;

    public Task<IvrPrompt?> GetPromptAsync(string promptId) => Task.FromResult<IvrPrompt?>(null);
    public Task<List<IvrPrompt>> GetAllPromptsAsync() => Task.FromResult(new List<IvrPrompt>());
    public Task<IvrPrompt> UpsertPromptAsync(IvrPrompt prompt) => Task.FromResult(prompt);
    public Task DeletePromptAsync(string id) => Task.CompletedTask;

    public Task<CallLog> CreateCallLogAsync(CallLog log) => Task.FromResult(log);
    public Task<CallLog> UpdateCallLogAsync(CallLog log) => Task.FromResult(log);
    public Task<List<CallLog>> GetCallLogsAsync(DateTime? from = null, DateTime? to = null, int pageSize = 50) => Task.FromResult(new List<CallLog>());
    public Task<CallLog?> GetCallLogByCallIdAsync(string callId) => Task.FromResult<CallLog?>(null);
    public Task<List<CallLog>> GetCallLogsByPhoneNumberAsync(string phoneNumber, int limit = 20, DateTime? from = null, DateTime? to = null) => Task.FromResult(new List<CallLog>());

    public Task<CallEvent?> GetCallEventAsync(string callId, string eventId) => Task.FromResult<CallEvent?>(null);
    public Task<CallEvent> RecordCallEventAsync(CallEvent callEvent) => Task.FromResult(callEvent);
    public Task<List<CallEvent>> GetCallEventsForCallAsync(string callId) => Task.FromResult(new List<CallEvent>());

    public Task<BusinessHoursConfig?> GetBusinessHoursAsync(string? id = null) => Task.FromResult<BusinessHoursConfig?>(null);
    public Task<BusinessHoursConfig> UpsertBusinessHoursAsync(BusinessHoursConfig config) => Task.FromResult(config);

    public Task<SystemConfig> GetSystemConfigAsync() => Task.FromResult(new SystemConfig());
    public Task<SystemConfig> UpsertSystemConfigAsync(SystemConfig config) => Task.FromResult(config);

    public Task<TeamRoutingConfig?> GetTeamRoutingConfigAsync(string id) => Task.FromResult<TeamRoutingConfig?>(null);
    public Task<List<TeamRoutingConfig>> GetTeamRoutingConfigsAsync(List<string>? ids = null) => Task.FromResult(new List<TeamRoutingConfig>());
    public Task<List<TeamRoutingConfig>> GetAllActiveTeamRoutingConfigsAsync() => Task.FromResult(new List<TeamRoutingConfig>());
    public Task<TeamRoutingConfig> UpsertTeamRoutingConfigAsync(TeamRoutingConfig config) => Task.FromResult(config);
    public Task DeleteTeamRoutingConfigAsync(string id) => Task.CompletedTask;

    public Task<ExternalSystemConfig?> GetExternalSystemConfigAsync(string id) => Task.FromResult<ExternalSystemConfig?>(null);
    public Task<List<ExternalSystemConfig>> GetAllActiveExternalSystemConfigsAsync() => Task.FromResult(new List<ExternalSystemConfig>());
    public Task<ExternalSystemConfig> UpsertExternalSystemConfigAsync(ExternalSystemConfig config) => Task.FromResult(config);
    public Task DeleteExternalSystemConfigAsync(string id) => Task.CompletedTask;

    public Task<DataExtractionConfig?> GetDataExtractionConfigAsync(string id) => Task.FromResult<DataExtractionConfig?>(null);
    public Task<List<DataExtractionConfig>> GetAllActiveDataExtractionConfigsAsync() => Task.FromResult(new List<DataExtractionConfig>());
    public Task<DataExtractionConfig> UpsertDataExtractionConfigAsync(DataExtractionConfig config) => Task.FromResult(config);
    public Task DeleteDataExtractionConfigAsync(string id) => Task.CompletedTask;

    public Task<PhoneNumberConfig?> GetPhoneNumberConfigAsync(string phoneNumber) => Task.FromResult<PhoneNumberConfig?>(null);
    public Task<PhoneNumberConfig?> GetPhoneNumberConfigByIdAsync(string id) => Task.FromResult<PhoneNumberConfig?>(null);
    public Task<List<PhoneNumberConfig>> GetAllPhoneNumberConfigsAsync(bool activeOnly = true) => Task.FromResult(new List<PhoneNumberConfig>());
    public Task<PhoneNumberConfig> UpsertPhoneNumberConfigAsync(PhoneNumberConfig config) => Task.FromResult(config);
    public Task DeletePhoneNumberConfigAsync(string id) => Task.CompletedTask;

    public Task<EventSchedule?> GetEventScheduleAsync(string id) => Task.FromResult<EventSchedule?>(null);
    public Task<List<EventSchedule>> GetActiveEventSchedulesForFacilityAsync(string facilityId, string? deviceId = null) => Task.FromResult(new List<EventSchedule>());
    public Task<EventSchedule> UpsertEventScheduleAsync(EventSchedule schedule) => Task.FromResult(schedule);
    public Task DeleteEventScheduleAsync(string id) => Task.CompletedTask;
}
