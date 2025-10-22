# Test the expanded Star Wars universe with all 9 films

Write-Host "`n🌌 Testing Expanded Star Wars Universe (9 Films)`n" -ForegroundColor Cyan

$query = @"
{
  films {
    items(orderBy: [{ episodeId: ASC }]) {
      title
      episodeId
      releaseDate
      director
      characters {
        totalCount
        items(take: 5) {
          name
          characterType
          homePlanet { name }
        }
      }
    }
  }
}
"@

$body = @{
    query = $query
} | ConvertTo-Json

Write-Host "📡 Querying all films with character counts..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/graphql" -Method Post -Body $body -ContentType "application/json"
    
    if ($response.errors) {
        Write-Host "❌ GraphQL Errors:" -ForegroundColor Red
        $response.errors | ConvertTo-Json -Depth 10
    }
    else {
        Write-Host "`n✅ Successfully retrieved all films!`n" -ForegroundColor Green
        
        $films = $response.data.films.items
        Write-Host "📊 Total Films: $($films.Count)" -ForegroundColor Cyan
        
        foreach ($film in $films) {
            Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
            Write-Host "Episode $($film.episodeId): $($film.title)" -ForegroundColor Yellow
            Write-Host "Director: $($film.director)" -ForegroundColor Gray
            Write-Host "Release: $($film.releaseDate)" -ForegroundColor Gray
            Write-Host "Characters: $($film.characters.totalCount) total" -ForegroundColor Cyan
            
            if ($film.characters.items.Count -gt 0) {
                Write-Host "`nFirst 5 Characters:" -ForegroundColor White
                foreach ($char in $film.characters.items) {
                    $planet = if ($char.homePlanet) { " (from $($char.homePlanet.name))" } else { "" }
                    Write-Host "  • $($char.name) - $($char.characterType)$planet" -ForegroundColor Gray
                }
            }
        }
        
        Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
        Write-Host "`n🎬 Summary:" -ForegroundColor Green
        Write-Host "  Prequel Trilogy (I-III): $($films | Where-Object { $_.episodeId -le 3 } | Measure-Object | Select-Object -ExpandProperty Count) films" -ForegroundColor Cyan
        Write-Host "  Original Trilogy (IV-VI): $($films | Where-Object { $_.episodeId -ge 4 -and $_.episodeId -le 6 } | Measure-Object | Select-Object -ExpandProperty Count) films" -ForegroundColor Cyan
        Write-Host "  Sequel Trilogy (VII-IX): $($films | Where-Object { $_.episodeId -ge 7 } | Measure-Object | Select-Object -ExpandProperty Count) films" -ForegroundColor Cyan
        
        $totalCharacters = ($films | ForEach-Object { $_.characters.totalCount } | Measure-Object -Sum).Sum
        Write-Host "`n  Total Characters Across All Films: $totalCharacters" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error querying GraphQL endpoint:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`n💡 Make sure the server is running with: .\run-server.ps1" -ForegroundColor Yellow
}
