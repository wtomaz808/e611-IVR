FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["IVR.McpServer/IVR.McpServer.csproj", "IVR.McpServer/"]
COPY ["IVR.Core/IVR.Core.csproj", "IVR.Core/"]
RUN dotnet restore "IVR.McpServer/IVR.McpServer.csproj"
COPY . .
WORKDIR "/src/IVR.McpServer"
RUN dotnet build "IVR.McpServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IVR.McpServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IVR.McpServer.dll"]
