# Restart EV Charging Demo
Write-Host "🔄 Restarting EV Charging Demo..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Find and stop any running server processes
Write-Host "1️⃣  Stopping any running servers..." -ForegroundColor Yellow
$serverProcesses = Get-Process | Where-Object { $_.ProcessName -like "*SharpGraph.Server*" -or $_.MainWindowTitle -like "*run-server*" }
if ($serverProcesses) {
    $serverProcesses | Stop-Process -Force
    Write-Host "   ✅ Stopped $($serverProcesses.Count) server process(es)" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "   ℹ️  No server processes found" -ForegroundColor Gray
}

# Step 2: Clean the database
Write-Host ""
Write-Host "2️⃣  Cleaning database..." -ForegroundColor Yellow
if (Test-Path ".\graphql_db") {
    Remove-Item -Recurse -Force ".\graphql_db\*" -ErrorAction SilentlyContinue
    Write-Host "   ✅ Database cleaned" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  No database to clean" -ForegroundColor Gray
}

# Step 3: Start the server
Write-Host ""
Write-Host "3️⃣  Starting server..." -ForegroundColor Yellow
Write-Host "   📝 Run this command in a new terminal:" -ForegroundColor Cyan
Write-Host "      .\run-server.ps1" -ForegroundColor White
Write-Host ""
Write-Host "   Then press Enter here to continue..." -ForegroundColor Yellow
Read-Host

# Step 4: Upload EV Charging data
Write-Host ""
Write-Host "4️⃣  Uploading EV Charging data..." -ForegroundColor Yellow
.\run-evcharging-client.ps1

Write-Host ""
Write-Host "✅ Done! Server is ready at http://localhost:8080/graphql" -ForegroundColor Green
