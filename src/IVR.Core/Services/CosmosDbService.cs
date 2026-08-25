using IVR.Core.Interfaces;
using IVR.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;

namespace IVR.Core.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly Container _aniContainer;
    private readonly Container _aliContainer;
    private readonly Container _menuContainer;
    private readonly Container _promptContainer;
    private readonly Container _callLogContainer;
    private readonly Container _configContainer;
    private readonly Container _teamRoutingContainer;
    private readonly Container _externalSystemContainer;
    private readonly Container _dataExtractionContainer;
    private readonly Container _phoneNumberContainer;
    private readonly Container _eventScheduleContainer;
    private readonly Container _callEventContainer;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger, string databaseName = "IvrDatabase")
    {
        _logger = logger;
        var database = cosmosClient.GetDatabase(databaseName);

        _aniContainer = database.GetContainer("AniRecords");
        _aliContainer = database.GetContainer("AliRecords");
        _menuContainer = database.GetContainer("Menus");
        _promptContainer = database.GetContainer("Prompts");
        _callLogContainer = database.GetContainer("CallLogs");
        _configContainer = database.GetContainer("Config");
        _teamRoutingContainer = database.GetContainer("TeamRouting");
        _externalSystemContainer = database.GetContainer("ExternalSystems");
        _dataExtractionContainer = database.GetContainer("DataExtraction");
        _phoneNumberContainer = database.GetContainer("PhoneNumbers");
        _eventScheduleContainer = database.GetContainer("EventSchedules");
        _callEventContainer = database.GetContainer("CallEvents");
    }

    // ─── Event Schedules ─────────────────────────────────────────────

    public async Task<EventSchedule?> GetEventScheduleAsync(string id)
    {
        try
        {
            var response = await _eventScheduleContainer.ReadItemAsync<EventSchedule>(id, new PartitionKey("event-schedule"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<EventSchedule>> GetActiveEventSchedulesForFacilityAsync(string facilityId, string? deviceId = null)
    {
        var queryText = deviceId == null
            ? "SELECT * FROM c WHERE c.isActive = true AND c.facilityId = @facilityId ORDER BY c.startTimeUtc"
            : "SELECT * FROM c WHERE c.isActive = true AND c.facilityId = @facilityId AND (c.deviceId = @deviceId OR NOT IS_DEFINED(c.deviceId)) ORDER BY c.startTimeUtc";
        var queryDef = new QueryDefinition(queryText).WithParameter("@facilityId", facilityId);
        if (deviceId != null)
        {
            queryDef = queryDef.WithParameter("@deviceId", deviceId);
        }

        var results = new List<EventSchedule>();
        using var iterator = _eventScheduleContainer.GetItemQueryIterator<EventSchedule>(queryDef);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<EventSchedule> UpsertEventScheduleAsync(EventSchedule schedule)
    {
        schedule.UpdatedAt = DateTime.UtcNow;
        var response = await _eventScheduleContainer.UpsertItemAsync(schedule, new PartitionKey(schedule.PartitionKey));
        _logger.LogInformation("Upserted event schedule {Id} for facility {FacilityId}", schedule.Id, schedule.FacilityId);
        return response.Resource;
    }

    public async Task DeleteEventScheduleAsync(string id)
    {
        await _eventScheduleContainer.DeleteItemAsync<EventSchedule>(id, new PartitionKey("event-schedule"));
        _logger.LogInformation("Deleted event schedule {Id}", id);
    }

    // ─── ANI Records ────────────────────────────────────────────────

    public async Task<AniRecord?> GetAniRecordAsync(string phoneNumber)
    {
        var query = _aniContainer.GetItemLinqQueryable<AniRecord>()
            .Where(a => a.PhoneNumber == phoneNumber)
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<List<AniRecord>> SearchAniRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null)
    {
        var queryDef = searchTerm != null
            ? new QueryDefinition("SELECT * FROM c WHERE CONTAINS(c.phoneNumber, @search) OR CONTAINS(c.callerName, @search)")
                .WithParameter("@search", searchTerm)
            : new QueryDefinition("SELECT * FROM c");

        var results = new List<AniRecord>();
        using var iterator = _aniContainer.GetItemQueryIterator<AniRecord>(queryDef, continuationToken,
            new QueryRequestOptions { MaxItemCount = pageSize });

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<AniRecord> UpsertAniRecordAsync(AniRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;
        var response = await _aniContainer.UpsertItemAsync(record, new PartitionKey(record.PartitionKey));
        _logger.LogInformation("Upserted ANI record for {PhoneNumber}", record.PhoneNumber);
        return response.Resource;
    }

    public async Task DeleteAniRecordAsync(string id, string partitionKey)
    {
        await _aniContainer.DeleteItemAsync<AniRecord>(id, new PartitionKey(partitionKey));
        _logger.LogInformation("Deleted ANI record {Id}", id);
    }

    // ─── ALI Records ────────────────────────────────────────────────

    public async Task<AliRecord?> GetAliRecordAsync(string phoneNumber)
    {
        var query = _aliContainer.GetItemLinqQueryable<AliRecord>()
            .Where(a => a.PhoneNumber == phoneNumber)
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<List<AliRecord>> SearchAliRecordsAsync(string? searchTerm = null, int pageSize = 50, string? continuationToken = null)
    {
        var queryDef = searchTerm != null
            ? new QueryDefinition("SELECT * FROM c WHERE CONTAINS(c.phoneNumber, @search) OR CONTAINS(c.address.city, @search)")
                .WithParameter("@search", searchTerm)
            : new QueryDefinition("SELECT * FROM c");

        var results = new List<AliRecord>();
        using var iterator = _aliContainer.GetItemQueryIterator<AliRecord>(queryDef, continuationToken,
            new QueryRequestOptions { MaxItemCount = pageSize });

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<AliRecord> UpsertAliRecordAsync(AliRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;
        var response = await _aliContainer.UpsertItemAsync(record, new PartitionKey(record.PartitionKey));
        _logger.LogInformation("Upserted ALI record for {PhoneNumber}", record.PhoneNumber);
        return response.Resource;
    }

    public async Task DeleteAliRecordAsync(string id, string partitionKey)
    {
        await _aliContainer.DeleteItemAsync<AliRecord>(id, new PartitionKey(partitionKey));
    }

    // ─── IVR Menus ──────────────────────────────────────────────────

    public async Task<IvrMenu?> GetMenuAsync(string menuId)
    {
        try
        {
            var response = await _menuContainer.ReadItemAsync<IvrMenu>(menuId, new PartitionKey("menu"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<IvrMenu>> GetAllMenusAsync()
    {
        var results = new List<IvrMenu>();
        using var iterator = _menuContainer.GetItemQueryIterator<IvrMenu>(
            new QueryDefinition("SELECT * FROM c ORDER BY c[\"order\"]"));

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<IvrMenu?> GetRootMenuAsync()
    {
        var query = _menuContainer.GetItemLinqQueryable<IvrMenu>()
            .Where(m => m.IsRootMenu && m.IsActive)
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<IvrMenu> UpsertMenuAsync(IvrMenu menu)
    {
        menu.UpdatedAt = DateTime.UtcNow;
        var response = await _menuContainer.UpsertItemAsync(menu, new PartitionKey(menu.PartitionKey));
        _logger.LogInformation("Upserted menu {MenuName} ({MenuId})", menu.Name, menu.Id);
        return response.Resource;
    }

    public async Task DeleteMenuAsync(string id)
    {
        await _menuContainer.DeleteItemAsync<IvrMenu>(id, new PartitionKey("menu"));
    }

    // ─── Prompts ────────────────────────────────────────────────────

    public async Task<IvrPrompt?> GetPromptAsync(string promptId)
    {
        try
        {
            var response = await _promptContainer.ReadItemAsync<IvrPrompt>(promptId, new PartitionKey("prompt"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<IvrPrompt>> GetAllPromptsAsync()
    {
        var results = new List<IvrPrompt>();
        using var iterator = _promptContainer.GetItemQueryIterator<IvrPrompt>(
            new QueryDefinition("SELECT * FROM c ORDER BY c.name"));

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<IvrPrompt> UpsertPromptAsync(IvrPrompt prompt)
    {
        prompt.UpdatedAt = DateTime.UtcNow;
        var response = await _promptContainer.UpsertItemAsync(prompt, new PartitionKey(prompt.PartitionKey));
        _logger.LogInformation("Upserted prompt {PromptName} ({PromptId})", prompt.Name, prompt.Id);
        return response.Resource;
    }

    public async Task DeletePromptAsync(string id)
    {
        await _promptContainer.DeleteItemAsync<IvrPrompt>(id, new PartitionKey("prompt"));
    }

    // ─── Call Logs ──────────────────────────────────────────────────

    public async Task<CallLog> CreateCallLogAsync(CallLog log)
    {
        var response = await _callLogContainer.CreateItemAsync(log, new PartitionKey(log.PartitionKey));
        return response.Resource;
    }

    public async Task<CallLog> UpdateCallLogAsync(CallLog log)
    {
        var response = await _callLogContainer.UpsertItemAsync(log, new PartitionKey(log.PartitionKey));
        return response.Resource;
    }

    public async Task<List<CallLog>> GetCallLogsAsync(DateTime? from = null, DateTime? to = null, int pageSize = 50)
    {
        from ??= DateTime.UtcNow.AddDays(-7);
        to ??= DateTime.UtcNow;

        var queryDef = new QueryDefinition(
            "SELECT * FROM c WHERE c.startTime >= @from AND c.startTime <= @to ORDER BY c.startTime DESC")
            .WithParameter("@from", from)
            .WithParameter("@to", to);

        var results = new List<CallLog>();
        using var iterator = _callLogContainer.GetItemQueryIterator<CallLog>(queryDef,
            requestOptions: new QueryRequestOptions { MaxItemCount = pageSize });

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    public async Task<CallLog?> GetCallLogByCallIdAsync(string callId)
    {
        var query = _callLogContainer.GetItemLinqQueryable<CallLog>()
            .Where(c => c.CallId == callId)
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<List<CallLog>> GetCallLogsByPhoneNumberAsync(string phoneNumber, int limit = 20, DateTime? from = null, DateTime? to = null)
    {
        from ??= DateTime.UtcNow.AddDays(-90);
        to ??= DateTime.UtcNow;

        var queryDef = new QueryDefinition(
            "SELECT * FROM c WHERE c.callerNumber = @phoneNumber AND c.startTime >= @from AND c.startTime <= @to ORDER BY c.startTime DESC")
            .WithParameter("@phoneNumber", phoneNumber)
            .WithParameter("@from", from)
            .WithParameter("@to", to);

        var results = new List<CallLog>();
        using var iterator = _callLogContainer.GetItemQueryIterator<CallLog>(queryDef,
            requestOptions: new QueryRequestOptions { MaxItemCount = limit });

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    // ─── Call Events ────────────────────────────────────────────────

    public async Task<CallEvent?> GetCallEventAsync(string callId, string eventId)
    {
        try
        {
            var response = await _callEventContainer.ReadItemAsync<CallEvent>(eventId, new PartitionKey(callId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<CallEvent> RecordCallEventAsync(CallEvent callEvent)
    {
        try
        {
            var response = await _callEventContainer.CreateItemAsync(callEvent, new PartitionKey(callEvent.PartitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Same eventId already recorded for this call — idempotent no-op, return the existing record.
            var existing = await GetCallEventAsync(callEvent.CallId, callEvent.Id);
            return existing ?? callEvent;
        }
    }

    public async Task<List<CallEvent>> GetCallEventsForCallAsync(string callId)
    {
        var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.callId = @callId ORDER BY c.timestampUtc")
            .WithParameter("@callId", callId);

        var results = new List<CallEvent>();
        using var iterator = _callEventContainer.GetItemQueryIterator<CallEvent>(queryDef, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(callId) });
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    // ─── Business Hours ─────────────────────────────────────────────

    public async Task<BusinessHoursConfig?> GetBusinessHoursAsync(string? id = null)
    {
        try
        {
            var docId = id ?? "default-business-hours";
            var response = await _configContainer.ReadItemAsync<BusinessHoursConfig>(docId, new PartitionKey("config"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<BusinessHoursConfig> UpsertBusinessHoursAsync(BusinessHoursConfig config)
    {
        var response = await _configContainer.UpsertItemAsync(config, new PartitionKey(config.PartitionKey));
        return response.Resource;
    }

    // ─── System Config ──────────────────────────────────────────────

    public async Task<SystemConfig> GetSystemConfigAsync()
    {
        try
        {
            var response = await _configContainer.ReadItemAsync<SystemConfig>("system-config", new PartitionKey("config"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var defaultConfig = new SystemConfig();
            await _configContainer.CreateItemAsync(defaultConfig, new PartitionKey(defaultConfig.PartitionKey));
            return defaultConfig;
        }
    }

    public async Task<SystemConfig> UpsertSystemConfigAsync(SystemConfig config)
    {
        var response = await _configContainer.UpsertItemAsync(config, new PartitionKey(config.PartitionKey));
        return response.Resource;
    }

    // ─── Team Routing ───────────────────────────────────────────────

    public async Task<TeamRoutingConfig?> GetTeamRoutingConfigAsync(string id)
    {
        try
        {
            var response = await _teamRoutingContainer.ReadItemAsync<TeamRoutingConfig>(id, new PartitionKey("team-routing"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<TeamRoutingConfig>> GetTeamRoutingConfigsAsync(List<string>? ids = null)
    {
        var results = new List<TeamRoutingConfig>();

        if (ids == null || ids.Count == 0)
        {
            return await GetAllActiveTeamRoutingConfigsAsync();
        }

        // Fetch specific configs by ID
        foreach (var id in ids)
        {
            var config = await GetTeamRoutingConfigAsync(id);
            if (config != null)
                results.Add(config);
        }

        return results.OrderByDescending(r => r.Priority).ToList();
    }

    public async Task<List<TeamRoutingConfig>> GetAllActiveTeamRoutingConfigsAsync()
    {
        var results = new List<TeamRoutingConfig>();
        var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.isActive = true ORDER BY c.priority DESC");

        using var iterator = _teamRoutingContainer.GetItemQueryIterator<TeamRoutingConfig>(queryDef);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<TeamRoutingConfig> UpsertTeamRoutingConfigAsync(TeamRoutingConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var response = await _teamRoutingContainer.UpsertItemAsync(config, new PartitionKey(config.PartitionKey));
        _logger.LogInformation("Upserted team routing config {TeamName} ({Id})", config.TeamName, config.Id);
        return response.Resource;
    }

    public async Task DeleteTeamRoutingConfigAsync(string id)
    {
        await _teamRoutingContainer.DeleteItemAsync<TeamRoutingConfig>(id, new PartitionKey("team-routing"));
        _logger.LogInformation("Deleted team routing config {Id}", id);
    }

    // ─── External Systems ───────────────────────────────────────────

    public async Task<ExternalSystemConfig?> GetExternalSystemConfigAsync(string id)
    {
        try
        {
            var response = await _externalSystemContainer.ReadItemAsync<ExternalSystemConfig>(id, new PartitionKey("external-system"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<ExternalSystemConfig>> GetAllActiveExternalSystemConfigsAsync()
    {
        var results = new List<ExternalSystemConfig>();
        var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.isActive = true ORDER BY c.systemName");

        using var iterator = _externalSystemContainer.GetItemQueryIterator<ExternalSystemConfig>(queryDef);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<ExternalSystemConfig> UpsertExternalSystemConfigAsync(ExternalSystemConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var response = await _externalSystemContainer.UpsertItemAsync(config, new PartitionKey(config.PartitionKey));
        _logger.LogInformation("Upserted external system config {SystemName} ({Id})", config.SystemName, config.Id);
        return response.Resource;
    }

    public async Task DeleteExternalSystemConfigAsync(string id)
    {
        await _externalSystemContainer.DeleteItemAsync<ExternalSystemConfig>(id, new PartitionKey("external-system"));
        _logger.LogInformation("Deleted external system config {Id}", id);
    }

    // ─── Data Extraction ────────────────────────────────────────────

    public async Task<DataExtractionConfig?> GetDataExtractionConfigAsync(string id)
    {
        try
        {
            var response = await _dataExtractionContainer.ReadItemAsync<DataExtractionConfig>(id, new PartitionKey("data-extraction"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<DataExtractionConfig>> GetAllActiveDataExtractionConfigsAsync()
    {
        var results = new List<DataExtractionConfig>();
        var queryDef = new QueryDefinition("SELECT * FROM c WHERE c.isActive = true ORDER BY c.name");

        using var iterator = _dataExtractionContainer.GetItemQueryIterator<DataExtractionConfig>(queryDef);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<DataExtractionConfig> UpsertDataExtractionConfigAsync(DataExtractionConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var response = await _dataExtractionContainer.UpsertItemAsync(config, new PartitionKey(config.PartitionKey));
        _logger.LogInformation("Upserted data extraction config {Name} ({Id})", config.Name, config.Id);
        return response.Resource;
    }

    public async Task DeleteDataExtractionConfigAsync(string id)
    {
        await _dataExtractionContainer.DeleteItemAsync<DataExtractionConfig>(id, new PartitionKey("data-extraction"));
        _logger.LogInformation("Deleted data extraction config {Id}", id);
    }

    // ─── Phone Number Config (PSTN / Direct Routing) ────────────────

    public async Task<PhoneNumberConfig?> GetPhoneNumberConfigAsync(string phoneNumber)
    {
        // Look up by phone number or by any alias
        var query = _phoneNumberContainer.GetItemLinqQueryable<PhoneNumberConfig>()
            .Where(p => p.PhoneNumber == phoneNumber || p.CalledNumberAliases.Contains(phoneNumber))
            .ToFeedIterator();

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault();
        }
        return null;
    }

    public async Task<PhoneNumberConfig?> GetPhoneNumberConfigByIdAsync(string id)
    {
        try
        {
            var response = await _phoneNumberContainer.ReadItemAsync<PhoneNumberConfig>(id, new PartitionKey("phone-number"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<PhoneNumberConfig>> GetAllPhoneNumberConfigsAsync(bool activeOnly = true)
    {
        var results = new List<PhoneNumberConfig>();
        var queryText = activeOnly
            ? "SELECT * FROM c WHERE c.isActive = true ORDER BY c.phoneNumber"
            : "SELECT * FROM c ORDER BY c.phoneNumber";

        using var iterator = _phoneNumberContainer.GetItemQueryIterator<PhoneNumberConfig>(new QueryDefinition(queryText));
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<PhoneNumberConfig> UpsertPhoneNumberConfigAsync(PhoneNumberConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var response = await _phoneNumberContainer.UpsertItemAsync(config, new PartitionKey(config.PartitionKey));
        _logger.LogInformation("Upserted phone number config {PhoneNumber} ({Id})", config.PhoneNumber, config.Id);
        return response.Resource;
    }

    public async Task DeletePhoneNumberConfigAsync(string id)
    {
        await _phoneNumberContainer.DeleteItemAsync<PhoneNumberConfig>(id, new PartitionKey("phone-number"));
        _logger.LogInformation("Deleted phone number config {Id}", id);
    }
}
