# Star Wars Client Runner
# Uploads Star Wars schema and data to the running server

Write-Host "Star Wars Database Schema Upload Client" -ForegroundColor Cyan
Write-Host ""

$projectPath = Join-Path $PSScriptRoot "examples\StarWars\StarWars.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Host "Error: Project file not found at $projectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Project: $projectPath" -ForegroundColor Green
Write-Host "Server: http://127.0.0.1:8080" -ForegroundColor Yellow
Write-Host ""
Write-Host "WARNING: Make sure the server is running first!" -ForegroundColor Yellow
Write-Host "   Run: .\run-server.ps1" -ForegroundColor Gray
Write-Host ""

# Run the client
dotnet run --project "$projectPath"

Write-Host ""
