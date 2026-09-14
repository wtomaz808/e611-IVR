# IVR Data Seeder

Console application to seed comprehensive test data into the e611-IVR system Cosmos DB.

## Features

- Seeds **31 test prompts** covering all e611-IVR scenarios
- Seeds **12 ANI records** (Automatic Number Identification - caller data)
- Seeds **12 ALI records** (Automatic Location Identification - location data)
- Supports both local development and Azure environments
- Upserts data (safe to run multiple times)
- Detailed console output with progress tracking
- Uses GUID format for all record IDs

## Data Created

### Prompts (31 total)

### Welcome (3 prompts)
- Main welcome message
- Business hours welcome
- After hours welcome

### Menu (5 prompts)
- Main menu with DTMF options
- Main menu with speech recognition
- Fire services submenu
- Police services submenu
- Medical services submenu

### Confirmation (6 prompts)
- Fire department confirmation
- Police department confirmation
- Medical/EMS confirmation
- Non-emergency confirmation
- Confirmation yes response
- Confirmation no response

### Transfer (4 prompts)
- Transfer to fire
- Transfer to police
- Transfer to medical
- Transfer to non-emergency

### Error (4 prompts)
- Invalid input
- No input detected
- Maximum retries reached
- System error

### Hold (2 prompts)
- Hold music message
- Queue position update

### Callback (2 prompts)
- Callback offer
- Callback confirmed

### Closing (2 prompts)
- Standard goodbye
- Goodbye with callback

### Special Scenarios (3 prompts)
- Emergency priority routing
- Caller location prompt
- Caller name prompt

### ANI Records (12 total)
ANI (Automatic Number Identification) records map phone numbers to caller information:

- **Residential Callers (3)**: Standard home phone numbers with caller names
- **Business Callers (3)**: City Hall (VIP), Hospital (VIP), Tech Corp
- **Government/Internal (3)**: Fire Station 5, Police Precinct 12, Public Works
- **Emergency Services (1)**: Emergency Dispatch Center
- **Blocked Callers (1)**: Test blocked spam caller
- **VIP Residential (1)**: Dr. Sarah Johnson (priority routing)

Features tested:
- CallerType: Residential, Business, Government, Internal, Emergency
- VIP flags for priority routing
- Blocked flags for spam prevention
- Custom metadata (departments, building info, contact emails)
- Multi-language support (English, Spanish)

### ALI Records (12 total)
ALI (Automatic Location Identification) records map phone numbers to physical locations:

- **Residential (4)**: Single family homes and apartments with full addresses
- **Commercial (3)**: City Hall, Hospital, Office buildings with floor/suite info
- **Government (3)**: Fire Station, Police Station, Public Works with facility details
- **Emergency (1)**: 9-1-1 Dispatch Center with redundancy info
- **VoIP/Unknown (1)**: Test case for imprecise location data

Features tested:
- Full Address data (street, city, state, zip, country)
- GPS Coordinates (latitude, longitude)
- LocationType: Residential, Commercial, VoIP
- Service Areas and fire districts
- Access notes (gate codes, floor numbers, emergency entrances)
- Special instructions for first responders
- Springfield, VA test data (Northern Virginia region)

## Usage

### Azure Environment (Recommended: Key Vault)

The connection string is stored in Key Vault rather than checked into `appsettings.json`.

1. Set `KeyVault:Uri` in `appsettings.json` (already configured for the shared dev vault):
   ```json
   { "KeyVault": { "Uri": "https://ivr-kv-hgknk444g237w.vault.usgovcloudapi.net/" } }
   ```
2. Sign in with an identity that has `Key Vault Secrets User` (or `Secrets Officer`) on that vault:
   ```powershell
   Connect-AzAccount -Environment AzureUSGovernment
   ```
3. Run the seeder — it fetches the `CosmosDbConnectionString` secret automatically via `DefaultAzureCredential`:
   ```bash
   cd src/IVR.DataSeeder
   dotnet run
   ```

### Azure Environment (Direct Connection String)

If you don't have Key Vault access, you can still supply the connection string directly —
just don't commit it to `appsettings.json`.

1. Get your Cosmos DB connection string:
   ```bash
   az cosmosdb keys list \
     --name ivr-dev-cosmos-4c5ax3aimbdsy \
     --resource-group rg-ivr-dev \
     --type connection-strings \
     --query "connectionStrings[0].connectionString" \
     --output tsv
   ```

2. Run the seeder with connection string:
   ```bash
   cd src/IVR.DataSeeder
   dotnet run --CosmosDb:ConnectionString="AccountEndpoint=https://..."
   ```

   Or set as environment variable:
   ```bash
   $env:CosmosDb__ConnectionString = "AccountEndpoint=https://..."
   dotnet run
   ```

### Local Development

1. Update `appsettings.Development.json`:
   ```json
   {
     "CosmosDb": {
       "ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=..."
     }
   }
   ```

2. Run the seeder:
   ```bash
   dotnet run
   ```

### Docker Compose Environment

If using in-memory Cosmos DB with Docker:
```bash
$env:CosmosDb__ConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
dotnet run
```

## Output

The seeder provides detailed console output:

```
╔══════════════════════════════════════════════════════════╗
║   e611-IVR System - Prompt Data Seeder                  ║
╚══════════════════════════════════════════════════════════╝

📦 Connecting to Cosmos DB...
   Database: ivr-db
   Container: Prompts

🌱 Seeding 28 test prompts...

  ✅ Main Welcome Message
     Category: Welcome | Type: Tts
     Text: "Thank you for calling Emergency Services. How may we help..."

  ✅ Business Hours Welcome
     Category: Welcome | Type: Tts
     Text: "Thank you for calling Emergency Services. Our office is op..."

...

═══════════════════════════════════════════════════════════
✨ Seeding complete!
   Success: 28 prompts
═══════════════════════════════════════════════════════════

Next steps:
  1. Navigate to Admin Portal → Prompts to view created prompts
  2. Use these prompts when building Call Flows
  3. Assign confirmation prompts to Team Routing rules
```

## Configuration

Configuration can be provided via:

1. **appsettings.json** - Default configuration (`KeyVault:Uri` recommended for secrets)
2. **appsettings.Development.json** - Development overrides
3. **Environment variables** - Use double underscore: `CosmosDb__ConnectionString`
4. **Command line** - `dotnet run --CosmosDb:ConnectionString="..."`

Priority order: Command line > Environment > appsettings.Development.json > appsettings.json.
Key Vault is only consulted when none of the above supply `CosmosDb:ConnectionString`.

## Exit Codes

- `0` - Success (all prompts seeded)
- `1` - Error (configuration missing or seeding failures)

## Notes

- All prompts use Text-to-Speech (TTS) with `en-US-JennyNeural` voice
- Prompts have unique, meaningful IDs for easy reference in Call Flows
- Tags enable filtering and organization in the Admin Portal
- Running multiple times is safe - upserts existing prompts
- Created/Updated timestamps are set automatically
