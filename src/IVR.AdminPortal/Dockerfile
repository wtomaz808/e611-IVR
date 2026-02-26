# ─────────────────────────────────────────────────────────────────
# IVR Admin Portal — Blazor Server (.NET 8)
# ─────────────────────────────────────────────────────────────────

# ── Stage 1: Build ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY IVR.Core/IVR.Core.csproj IVR.Core/
COPY IVR.AdminPortal/IVR.AdminPortal.csproj IVR.AdminPortal/
RUN dotnet restore IVR.AdminPortal/IVR.AdminPortal.csproj

# Copy source and build
COPY IVR.Core/ IVR.Core/
COPY IVR.AdminPortal/ IVR.AdminPortal/
RUN dotnet publish IVR.AdminPortal/IVR.AdminPortal.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IVR.AdminPortal.dll"]
