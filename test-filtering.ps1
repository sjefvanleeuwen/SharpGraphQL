# Test GraphQL Filtering Queries

# Test 1: Find characters with "Skywalker" in name
$body1 = @{
    query = "query { characters(where: { name: { contains: `"Skywalker`" } }) { id name characterType } }"
} | ConvertTo-Json

Write-Host "`n=== Test 1: Find Skywalkers ===" -ForegroundColor Cyan
$response1 = Invoke-WebRequest -Uri "http://127.0.0.1:8080/graphql" -Method POST -ContentType "application/json" -Body $body1
$response1.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Test 2: Find characters taller than 170cm
$body2 = @{
    query = "query { characters(where: { height: { gt: 170 } }) { id name height } }"
} | ConvertTo-Json

Write-Host "`n=== Test 2: Characters taller than 170cm ===" -ForegroundColor Cyan
$response2 = Invoke-WebRequest -Uri "http://127.0.0.1:8080/graphql" -Method POST -ContentType "application/json" -Body $body2
$response2.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Test 3: Find films in original trilogy (episodes 4-6)
$body3 = @{
    query = "query { films(where: { episodeId: { gte: 4, lte: 6 } }) { id title episodeId } }"
} | ConvertTo-Json

Write-Host "`n=== Test 3: Original Trilogy Films ===" -ForegroundColor Cyan
$response3 = Invoke-WebRequest -Uri "http://127.0.0.1:8080/graphql" -Method POST -ContentType "application/json" -Body $body3
$response3.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Test 4: Find droids (with sorting)
$body4 = @{
    query = "query { characters(where: { characterType: `"Droid`" }, orderBy: { name: `"asc`" }) { id name primaryFunction } }"
} | ConvertTo-Json

Write-Host "`n=== Test 4: Droids (sorted by name) ===" -ForegroundColor Cyan
$response4 = Invoke-WebRequest -Uri "http://127.0.0.1:8080/graphql" -Method POST -ContentType "application/json" -Body $body4
$response4.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Test 5: Pagination - First 3 characters
$body5 = @{
    query = "query { characters(orderBy: { name: `"asc`" }, take: 3) { id name } }"
} | ConvertTo-Json

Write-Host "`n=== Test 5: First 3 Characters ===" -ForegroundColor Cyan
$response5 = Invoke-WebRequest -Uri "http://127.0.0.1:8080/graphql" -Method POST -ContentType "application/json" -Body $body5
$response5.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Test 6: Complex filter with AND/OR
$body6 = @{
    query = "query { characters(where: { OR: [{ characterType: `"Droid`" }, { height: { gt: 200 } }] }) { id name characterType height } }"
} | ConvertTo-Json

Write-Host "`n=== Test 6: Droids OR tall characters (>200cm) ===" -ForegroundColor Cyan
$response6 = Invoke-WebRequest -Uri "http://127.0.0.1:8080/graphql" -Method POST -ContentType "application/json" -Body $body6
$response6.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

Write-Host "`n=== All Tests Complete! ===" -ForegroundColor Green
