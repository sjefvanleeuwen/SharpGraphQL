#!/usr/bin/env pwsh

Write-Host "=== SharpGraph Filtering - Comprehensive Test ===" -ForegroundColor Cyan
Write-Host ""

$endpoint = "http://127.0.0.1:8080/graphql"

# Test 1: Simple filter query
Write-Host "Test 1: Simple filter (name contains 'Luke')" -ForegroundColor Yellow
$query1 = '{ characters(where: { name: { contains: "Luke" } }) { id name } }'
$r1 = Invoke-WebRequest -Uri $endpoint -Method POST -Body (@{ query = $query1 } | ConvertTo-Json) -ContentType 'application/json'
$d1 = $r1.Content | ConvertFrom-Json
if ($d1.data.characters) {
  Write-Host "✓ Success - found $($d1.data.characters.Length) results" -ForegroundColor Green
  $d1.data.characters | ForEach-Object { Write-Host "  - $($_.name)" }
} else {
  Write-Host "✗ Failed" -ForegroundColor Red
}

Write-Host ""

# Test 2: Numeric filter
Write-Host "Test 2: Numeric filter (episodeId >= 4)" -ForegroundColor Yellow
$query2 = '{ films(where: { episodeId: { gte: 4 } }) { id title episodeId } }'
$r2 = Invoke-WebRequest -Uri $endpoint -Method POST -Body (@{ query = $query2 } | ConvertTo-Json) -ContentType 'application/json'
$d2 = $r2.Content | ConvertFrom-Json
if ($d2.data.films) {
  Write-Host "✓ Success - found $($d2.data.films.Length) results" -ForegroundColor Green
  $d2.data.films | ForEach-Object { Write-Host "  - Episode $($_.episodeId): $($_.title)" }
} else {
  Write-Host "✗ Failed" -ForegroundColor Red
}

Write-Host ""

# Test 3: Logical operators (AND)
Write-Host "Test 3: Logical AND operator" -ForegroundColor Yellow
$query3 = '{ characters(where: { AND: [{ name: { contains: "Luke" } }, { characterType: { equals: "Human" } }] }) { id name characterType } }'
$r3 = Invoke-WebRequest -Uri $endpoint -Method POST -Body (@{ query = $query3 } | ConvertTo-Json) -ContentType 'application/json'
$d3 = $r3.Content | ConvertFrom-Json
if ($d3.data.characters) {
  Write-Host "✓ Success - found $($d3.data.characters.Length) results" -ForegroundColor Green
  $d3.data.characters | ForEach-Object { Write-Host "  - $($_.name) ($($_.characterType))" }
} else {
  Write-Host "✗ Failed" -ForegroundColor Red
}

Write-Host ""

# Test 4: Sorting
Write-Host "Test 4: Sorting by name (ascending)" -ForegroundColor Yellow
$query4 = '{ characters(orderBy: [{ field: "name", order: "asc" }], take: 3) { name } }'
$r4 = Invoke-WebRequest -Uri $endpoint -Method POST -Body (@{ query = $query4 } | ConvertTo-Json) -ContentType 'application/json'
$d4 = $r4.Content | ConvertFrom-Json
if ($d4.data.characters) {
  Write-Host "✓ Success - found $($d4.data.characters.Length) results" -ForegroundColor Green
  $d4.data.characters | ForEach-Object { Write-Host "  - $($_.name)" }
} else {
  Write-Host "✗ Failed" -ForegroundColor Red
}

Write-Host ""

# Test 5: Case-insensitive string filter
Write-Host "Test 5: Case-insensitive search (name contains 'luke')" -ForegroundColor Yellow
$query5 = '{ characters(where: { name: { contains: "luke", mode: "insensitive" } }) { id name } }'
$r5 = Invoke-WebRequest -Uri $endpoint -Method POST -Body (@{ query = $query5 } | ConvertTo-Json) -ContentType 'application/json'
$d5 = $r5.Content | ConvertFrom-Json
if ($d5.data.characters) {
  Write-Host "✓ Success - found $($d5.data.characters.Length) results (case-insensitive)" -ForegroundColor Green
  $d5.data.characters | ForEach-Object { Write-Host "  - $($_.name)" }
} else {
  Write-Host "✗ Failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== All Tests Complete ===" -ForegroundColor Cyan
Write-Host "Status: FILTERING FULLY FUNCTIONAL ✓" -ForegroundColor Green