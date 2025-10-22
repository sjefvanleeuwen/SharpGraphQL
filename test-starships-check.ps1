# Check if Character has starshipIds

$query = @{
    query = @"
{
  character(id: "luke") {
    id
    name
    starshipIds
  }
}
"@
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8080/graphql" -Method Post -Body $query -ContentType "application/json"
Write-Host ($response | ConvertTo-Json -Depth 10)
