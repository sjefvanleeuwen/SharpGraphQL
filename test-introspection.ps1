# Test introspection for filter types

Write-Host "Testing GraphQL Introspection..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Check Query type fields and their arguments
Write-Host "Test 1: Check Query.characters field arguments" -ForegroundColor Yellow
$query1 = @{
    query = @"
{
  __type(name: "Query") {
    fields {
      name
      args {
        name
        type {
          name
          kind
        }
      }
    }
  }
}
"@
}

$result1 = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body ($query1 | ConvertTo-Json) -ContentType 'application/json'
$charactersField = $result1.data.__type.fields | Where-Object { $_.name -eq "characters" }
Write-Host "Characters field arguments:" -ForegroundColor Green
$charactersField.args | ForEach-Object { Write-Host "  - $($_.name): $($_.type.name) ($($_.type.kind))" }

Write-Host ""

# Test 2: Check if CharacterWhereInput exists
Write-Host "Test 2: Check CharacterWhereInput type" -ForegroundColor Yellow
$query2 = @{
    query = @"
{
  __type(name: "CharacterWhereInput") {
    name
    kind
    inputFields {
      name
      type {
        name
        kind
      }
    }
  }
}
"@
}

$result2 = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body ($query2 | ConvertTo-Json) -ContentType 'application/json'
if ($result2.data.__type) {
    Write-Host "CharacterWhereInput found!" -ForegroundColor Green
    Write-Host "Input fields:" -ForegroundColor Green
    $result2.data.__type.inputFields | ForEach-Object { Write-Host "  - $($_.name): $($_.type.name) ($($_.type.kind))" }
} else {
    Write-Host "CharacterWhereInput NOT FOUND!" -ForegroundColor Red
}

Write-Host ""

# Test 3: Check all available types
Write-Host "Test 3: List all types containing 'Filter' or 'Where'" -ForegroundColor Yellow
$query3 = @{
    query = @"
{
  __schema {
    types {
      name
      kind
    }
  }
}
"@
}

$result3 = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body ($query3 | ConvertTo-Json) -ContentType 'application/json'
$filterTypes = $result3.data.__schema.types | Where-Object { $_.name -like "*Filter*" -or $_.name -like "*Where*" -or $_.name -like "*OrderBy*" }
Write-Host "Filter-related types:" -ForegroundColor Green
$filterTypes | ForEach-Object { Write-Host "  - $($_.name) ($($_.kind))" }
