# Run SharpGraph Server with visible output

Write-Host "Starting SharpGraph Server..." -ForegroundColor Cyan

# Kill any existing server processes
Get-Process | Where-Object { $_.ProcessName -like "*SharpGraph.Server*" } | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 1

Write-Host "Building and starting server..." -ForegroundColor Yellow
Write-Host "Database initialization and schema loading will be shown below:" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Gray

# Start server in current window with visible output
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Run the server and show output
dotnet run --project src\SharpGraph.Server --configuration Debug
