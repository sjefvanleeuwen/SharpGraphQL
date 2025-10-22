# Test EV Charging Introspection
$query = @"
{
  __schema {
    queryType {
      fields {
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

$body = @{ query = $query } | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://127.0.0.1:8080/graphql" -Method Post -Body $body -ContentType "application/json"
    
    Write-Host "`n📊 Available Query Fields:" -ForegroundColor Cyan
    Write-Host "=" * 60
    
    foreach ($field in $response.data.__schema.queryType.fields) {
        Write-Host "  - $($field.name) : $($field.type.name) ($($field.type.kind))" -ForegroundColor Green
    }
    
} catch {
    Write-Host "`n❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
