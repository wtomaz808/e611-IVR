# Starting the E911 IVR System Locally

This guide provides step-by-step instructions to launch all containers needed to run the E911 IVR solution locally.

## Prerequisites

- **Docker Desktop** installed and running on Windows
- **Git** (to clone/access the repository)
- **.env file** configured with required environment variables

## Quick Start

To start all services with a single command:

```powershell
docker compose up --build -d
```

This command will:
- Build all container images
- Start all services in detached mode (background)
- Create the necessary Docker network

## System Components

The local environment consists of four main containers:

| Container | Service | Port | Description |
|-----------|---------|------|-------------|
| `ivr-functions` | Azure Functions | 7071 | Main IVR call handling logic |
| `ivr-admin` | Admin Portal | 8080 | Blazor-based administration interface |
| `pstn-simulator` | PSTN Simulator | 5200 | Mock PSTN/ACS for testing calls |
| `azurite` | Storage Emulator | 10000-10002 | Local Azure Storage emulator |

## Detailed Steps

### 1. Configure Environment Variables

Before starting the containers, ensure you have a `.env` file in the root directory:

```powershell
# Copy the example file if you haven't already
cp .env.example .env
```

Edit `.env` and configure the required variables:

```bash
# Minimal configuration for local development
AZURE_WEBJOBS_STORAGE=UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://azurite
CALLBACK_BASE_URL=http://localhost:7071
ASPNETCORE_ENVIRONMENT=Development

# Optional: Add Azure service credentials if needed
# ACS_CONNECTION_STRING=endpoint=https://...
# COSMOS_DB_CONNECTION_STRING=AccountEndpoint=...
# AZURE_OPENAI_ENDPOINT=https://...
```

### 2. Build and Start All Containers

From the repository root directory:

```powershell
# Navigate to the project root
cd c:\DSOP\repos3\IVR\e911-ivr

# Build and start all containers
docker compose up --build -d
```

The `--build` flag ensures images are rebuilt with the latest code changes.
The `-d` flag runs containers in detached mode (background).

### 3. Verify Container Status

Check that all containers are running:

```powershell
docker compose ps
```

You should see all four containers with status "Up":
- `ivr-functions`
- `ivr-admin`
- `pstn-simulator`
- `azurite`

### 4. Access the Services

Once all containers are running, you can access:

- **Admin Portal**: http://localhost:8080
- **PSTN Simulator**: http://localhost:5200
- **IVR Functions**: http://localhost:7071
- **Azurite Blob Storage**: http://localhost:10000
- **Azurite Queue Storage**: http://localhost:10001
- **Azurite Table Storage**: http://localhost:10002

### 5. View Container Logs

To monitor logs from all containers:

```powershell
docker compose logs -f
```

To view logs from a specific service:

```powershell
docker compose logs -f ivr-functions
docker compose logs -f ivr-admin
docker compose logs -f pstn-simulator
docker compose logs -f azurite
```

Press `Ctrl+C` to stop tailing logs.

## Stopping the System

### Stop All Containers

```powershell
docker compose stop
```

This stops all containers but preserves their state.

### Stop and Remove Containers

```powershell
docker compose down
```

This stops and removes containers, networks, but keeps volumes.

### Complete Cleanup

```powershell
# Remove containers, networks, and volumes
docker compose down -v

# Remove built images as well
docker compose down -v --rmi all
```

## Restarting the System

### Restart Without Rebuilding

If no code changes were made:

```powershell
docker compose up -d
```

### Restart with Rebuild

After making code changes:

```powershell
docker compose up --build -d
```

### Restart a Single Service

```powershell
# Stop and remove a specific container
docker compose stop ivr-functions
docker compose rm -f ivr-functions

# Rebuild and restart it
docker compose up --build -d ivr-functions
```

## Updating from Main Branch

When updates are available on the main branch, follow these steps to update your local environment and Azure deployments:

### 1. Pull Latest Changes

```powershell
# Stash any current work
git stash push -m "WIP: Before updating from main"

# Fetch and merge latest from main
git fetch origin
git merge origin/main -m "Merge main updates"

# Re-apply your stashed changes
git stash pop
```

### 2. Update Local Containers

After merging updates, rebuild affected containers:

```powershell
# Rebuild all containers with latest code
docker compose up --build -d

# Or rebuild specific containers only
docker compose up --build -d pstn-simulator ivr-admin
```

### 3. Deploy to Azure (If Applicable)

If you need to deploy the updates to Azure Government:

**Deploy Function App:**
```powershell
cd src/IVR.Functions
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..
az functionapp deployment source config-zip `
  --resource-group rg-ivr-dev `
  --name ivr-dev-func-4c5ax3aimbdsy `
  --src deploy.zip
```

**Deploy Admin Portal:**
```powershell
cd src/IVR.AdminPortal
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..
az webapp deployment source config-zip `
  --resource-group rg-ivr-dev `
  --name ivr-dev-admin-4c5ax3aimbdsy `
  --src deploy.zip
```

> **Note**: Ensure you're connected to Azure Government cloud before deploying:
> ```powershell
> az cloud set --name AzureUSGovernment
> az login --tenant d14ab12e-c535-4865-a593-c4115e7de102
> ```

### 4. Verify Updates

After updating:

```powershell
# Check container status
docker compose ps

# View logs for any errors
docker compose logs -f

# Test endpoints
# Admin Portal: http://localhost:8080
# PSTN Simulator: http://localhost:5200
# Functions: http://localhost:7071
```

## Troubleshooting

### Port Conflicts

If you see port binding errors, ensure the required ports are not in use:

```powershell
# Check if ports are already in use
netstat -ano | findstr ":7071"
netstat -ano | findstr ":8080"
netstat -ano | findstr ":5200"
netstat -ano | findstr ":10000"
```

### Container Build Failures

If a container fails to build:

1. Check the build logs:
   ```powershell
   docker compose build --no-cache ivr-functions
   ```

2. Verify Docker has enough resources (CPU, Memory, Disk)

3. Clean up Docker system:
   ```powershell
   docker system prune -a
   ```

### Connection Issues Between Containers

Containers communicate via the `ivr-network` Docker network. Verify it exists:

```powershell
docker network ls | findstr ivr-network
```

### Azurite Storage Not Accessible

If functions can't connect to Azurite, ensure:
- Azurite container is running and healthy
- `AZURE_WEBJOBS_STORAGE` points to the correct proxy URI
- The `azurite` service started before the functions container

## Development Workflow

### Typical Development Cycle

1. Make code changes in your IDE
2. Rebuild specific container(s):
   ```powershell
   docker compose up --build -d ivr-functions
   ```
3. Test via Admin Portal or PSTN Simulator
4. View logs to debug issues:
   ```powershell
   docker compose logs -f ivr-functions
   ```

### Working with the PSTN Simulator

The PSTN Simulator provides a mock ACS environment for testing calls without Azure resources:

1. Navigate to http://localhost:5200
2. Configure a test call scenario
3. Initiate calls to test IVR flow
4. Monitor events and responses in real-time

### Working with the Admin Portal

The Admin Portal allows you to:

- Manage call flows and prompts
- Configure ANI/ALI mappings
- View call logs and analytics
- Configure Teams integration
- Manage system settings

Access it at http://localhost:8080

## Network Architecture

```
┌─────────────────────────────────────────────────┐
│ Docker Network: ivr-network                     │
│                                                 │
│  ┌──────────────┐      ┌──────────────┐        │
│  │ ivr-admin    │      │ ivr-functions│        │
│  │ :8080        │      │ :80 (7071)   │        │
│  └──────┬───────┘      └──────┬───────┘        │
│         │                     │                 │
│         │    ┌────────────────┤                 │
│         │    │                │                 │
│  ┌──────┴────┴───┐     ┌──────┴───────┐        │
│  │ pstn-simulator│     │   azurite    │        │
│  │ :8080 (5200)  │     │ :10000-10002 │        │
│  └───────────────┘     └──────────────┘        │
│                                                 │
└─────────────────────────────────────────────────┘
         │                     │
    Host :5200            Host :7071
    Host :8080            Host :10000-10002
```

## Additional Resources

- [Architecture Documentation](./docs/architecture.md)
- [Function App Details](./docs/function-app.md)
- [Admin Portal Guide](./docs/admin-portal.md)
- [PSTN Simulator Guide](./docs/pstn-simulator.md)
- [System Integration](./docs/system-integration.md)

## Environment Variables Reference

See [.env.example](./.env.example) for a complete list of available environment variables and their descriptions.

### Required for Local Development

- `AZURE_WEBJOBS_STORAGE` - Storage connection for Functions runtime
- `ASPNETCORE_ENVIRONMENT` - ASP.NET environment (Development/Production)

### Optional for Full Functionality

- `ACS_CONNECTION_STRING` - Azure Communication Services
- `COSMOS_DB_CONNECTION_STRING` - Cosmos DB for state storage
- `STORAGE_CONNECTION_STRING` - Azure Storage for media files
- `COGNITIVE_SERVICES_ENDPOINT` - Speech services
- `AZURE_OPENAI_ENDPOINT` - GPT integration
- `AZURE_AD_TENANT_ID` - Admin portal authentication
- `APPINSIGHTS_CONNECTION_STRING` - Telemetry and monitoring

---

**Last Updated**: March 4, 2026  
**System Version**: 1.0  
**Docker Compose Version**: 3.8
