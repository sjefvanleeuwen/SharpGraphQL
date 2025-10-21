# Check what introspection returns for the characters field

$query = @{
    query = @"
{
  __type(name: "Query") {
    name
    fields {
      name
      args {
        name
        description
        type {
          kind
          name
          ofType {
            kind
            name
          }
        }
      }
    }
  }
}
"@
}

Write-Host "Fetching introspection for Query.characters field..." -ForegroundColor Cyan
$result = Invoke-RestMethod -Uri 'http://127.0.0.1:8080/graphql' -Method POST -Body ($query | ConvertTo-Json) -ContentType 'application/json'

$charactersField = $result.data.__type.fields | Where-Object { $_.name -eq "characters" }

Write-Host "`nQuery.characters field arguments:" -ForegroundColor Green
$charactersField.args | ForEach-Object {
    Write-Host "  - $($_.name): $($_.type.name) ($($_.type.kind))" -ForegroundColor Yellow
    Write-Host "    Description: $($_.description)" -ForegroundColor Gray
}

Write-Host "`nFull JSON response:" -ForegroundColor Cyan
$result | ConvertTo-Json -Depth 10
