# Simple script to copy in-memory seed data to Azure Cosmos DB
# This uses the same data structure that works in the local in-memory service

param(
    [Parameter(Mandatory=$true)]
    [string]$CosmosConnectionString
)

$env:CosmosDb__ConnectionString = $CosmosConnectionString

Write-Host "Starting data seeder with simplified approach..." -ForegroundColor Green

# Run the seeder with the connection string
dotnet run --project "c:\DSOP\repos3\IVR\e911-ivr\src\IVR.DataSeeder\IVR.DataSeeder.csproj"
