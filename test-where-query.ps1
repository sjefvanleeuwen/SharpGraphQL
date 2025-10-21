# Test the where clause directly with the server

Write-Host "Testing WHERE clause queries..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Simple contains filter
Write-Host "Test 1: Find characters with 'Skywalker' in name" -ForegroundColor Yellow
$query1 = @{
    query = 'query { characters(where: { name: { contains: "Skywalker" } }) { id name height } }'
}
$body1 = $query1 | ConvertTo-Json
Write-Host "Sending: $body1" -ForegroundColor Gray
$result1 = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body $body1 -ContentType 'application/json'
$result1.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
Write-Host ""

# Test 2: Numeric comparison
Write-Host "Test 2: Find characters taller than 170" -ForegroundColor Yellow
$query2 = @{
    query = 'query { characters(where: { height: { gt: 170 } }) { id name height } }'
}
$body2 = $query2 | ConvertTo-Json
$result2 = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body $body2 -ContentType 'application/json'
$result2.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
Write-Host ""

# Test 3: AND condition
Write-Host "Test 3: Find humans taller than 170" -ForegroundColor Yellow
$query3 = @{
    query = 'query { characters(where: { AND: [{ characterType: { equals: "Human" } }, { height: { gt: 170 } }] }) { id name height characterType } }'
}
$body3 = $query3 | ConvertTo-Json
$result3 = Invoke-WebRequest -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body $body3 -ContentType 'application/json'
$result3.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
Write-Host ""

Write-Host "All tests completed!" -ForegroundColor Green
