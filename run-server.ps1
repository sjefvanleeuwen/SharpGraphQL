# Restart SharpGraph Server

Write-Host "Restarting SharpGraph Server..." -ForegroundColor Cyan

# Kill any existing server processes
Get-Process | Where-Object { $_.ProcessName -like "*SharpGraph.Server*" } | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep -Seconds 1

# Start server in new window
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$scriptPath'; Write-Host 'Building and starting server...' -ForegroundColor Yellow; dotnet run --project src\SharpGraph.Server --configuration Release"

Write-Host "Server starting in new window!" -ForegroundColor Green
Write-Host "Waiting for server to be ready..." -ForegroundColor Yellow

# Wait for server to be ready
$ready = $false
$attempts = 0
$maxAttempts = 15

while (-not $ready -and $attempts -lt $maxAttempts) {
    Start-Sleep -Seconds 1
    $attempts++
    
    try {
        $response = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/graphql?sdl' -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
        }
    } catch {
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if ($ready) {
    Write-Host ""
    Write-Host "Server is ready at http://127.0.0.1:8080/graphql" -ForegroundColor Green
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Cyan
    Write-Host "Server is running!" -ForegroundColor Green
    Write-Host "Check SERVER_GUIDE.md for more examples" -ForegroundColor Yellow
    Write-Host "Server window is open - close it to stop" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "Server failed to start within $maxAttempts seconds" -ForegroundColor Red
    Write-Host "Check the server window for errors" -ForegroundColor Yellow
}
