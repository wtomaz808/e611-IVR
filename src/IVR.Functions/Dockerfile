# ─────────────────────────────────────────────────────────────────
# IVR Functions — Azure Functions v4 (.NET 8 Isolated Worker)
# ─────────────────────────────────────────────────────────────────

# ── Stage 1: Build ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY IVR.Core/IVR.Core.csproj IVR.Core/
COPY IVR.Functions/IVR.Functions.csproj IVR.Functions/
RUN dotnet restore IVR.Functions/IVR.Functions.csproj

# Copy source and build
COPY IVR.Core/ IVR.Core/
COPY IVR.Functions/ IVR.Functions/
RUN dotnet publish IVR.Functions/IVR.Functions.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────
FROM --platform=linux/amd64 mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=build /app/publish /home/site/wwwroot

EXPOSE 80
