# Run EV Charging Client - Uploads schema and 100,000+ records to server
Write-Host "EV Charging Client" -ForegroundColor Cyan
Write-Host ""

$projectPath = "examples/EVCharging/EVCharging.csproj"
$csprojContent = Get-Content $projectPath -Raw

# Temporarily modify csproj to use EVChargingClient.cs
$modifiedContent = $csprojContent -replace '<Compile Remove="EVChargingClient.cs" />', '<!-- <Compile Remove="EVChargingClient.cs" /> -->'
$modifiedContent = $modifiedContent -replace '<!-- <Compile Remove="Program.cs" /> -->', '<Compile Remove="Program.cs" />'

# Save temporarily
Set-Content $projectPath $modifiedContent

try {
    # Build and run
    Write-Host "Building project..." -ForegroundColor Yellow
    dotnet build $projectPath --configuration Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Starting client..." -ForegroundColor Green
        Write-Host ""
        dotnet run --project $projectPath --configuration Release --no-build
    } else {
        Write-Host "Build failed!" -ForegroundColor Red
    }
} finally {
    # Restore original csproj
    Set-Content $projectPath $csprojContent
}
