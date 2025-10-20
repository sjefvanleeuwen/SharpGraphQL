using SharpGraph.Examples;

namespace SharpGraph.Examples;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║   🌟 STAR WARS GRAPHQL DATABASE 🌟   ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");
        
        // Initialize Star Wars database
        var db = new StarWarsDatabase("starwars_db");
        
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("EXAMPLE QUERIES");
        Console.WriteLine(new string('=', 60) + "\n");
        
        // Query 1: Get Luke with his friends
        Console.WriteLine("📝 Query 1: Get Luke Skywalker with his friends\n");
        var query1 = @"
{
  character(id: ""luke"") {
    name
    height
    mass
    hairColor
    eyeColor
    birthYear
    friends {
      name
      characterType
    }
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query1);
        Console.WriteLine("\nResult:");
        var result1 = db.Query(query1);
        Console.WriteLine(result1.RootElement.GetRawText());
        
        // Query 2: Get all characters from A New Hope
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 2: Get characters from Episode IV: A New Hope\n");
        var query2 = @"
{
  characters {
    name
    characterType
    appearsIn
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query2);
        Console.WriteLine("\nResult (first 5):");
        var result2 = db.Query(query2);
        var data = result2.RootElement.GetProperty("data").GetProperty("characters");
        int count = 0;
        foreach (var character in data.EnumerateArray())
        {
            if (count++ >= 5) break;
            Console.WriteLine($"  - {character.GetProperty("name").GetString()} ({character.GetProperty("characterType").GetString()})");
            var appearsIn = character.GetProperty("appearsIn");
            Console.Write("    Appears in: ");
            foreach (var episode in appearsIn.EnumerateArray())
            {
                Console.Write($"{episode.GetString()} ");
            }
            Console.WriteLine();
        }
        
        // Query 3: Get R2-D2 with details
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 3: Get R2-D2 (Droid) with primary function\n");
        var query3 = @"
{
  character(id: ""r2d2"") {
    name
    characterType
    primaryFunction
    height
    mass
    eyeColor
    appearsIn
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query3);
        Console.WriteLine("\nResult:");
        var result3 = db.Query(query3);
        Console.WriteLine(result3.RootElement.GetRawText());
        
        // Query 4: Get all films
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 4: Get all Star Wars films\n");
        var query4 = @"
{
  films {
    title
    episodeId
    director
    releaseDate
    openingCrawl
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query4);
        Console.WriteLine("\nResult:");
        var result4 = db.Query(query4);
        var films = result4.RootElement.GetProperty("data").GetProperty("films");
        foreach (var film in films.EnumerateArray())
        {
            Console.WriteLine($"\n🎬 Episode {film.GetProperty("episodeId").GetInt32()}: {film.GetProperty("title").GetString()}");
            Console.WriteLine($"   Director: {film.GetProperty("director").GetString()}");
            Console.WriteLine($"   Released: {film.GetProperty("releaseDate").GetString()}");
            Console.WriteLine($"   Opening: {film.GetProperty("openingCrawl").GetString()[..100]}...");
        }
        
        // Query 5: Get planets
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 5: Get famous planets\n");
        var query5 = @"
{
  planets {
    name
    climate
    terrain
    population
    diameter
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query5);
        Console.WriteLine("\nResult:");
        var result5 = db.Query(query5);
        var planets = result5.RootElement.GetProperty("data").GetProperty("planets");
        int pCount = 0;
        foreach (var planet in planets.EnumerateArray())
        {
            if (pCount++ >= 5) break;
            Console.WriteLine($"\n🪐 {planet.GetProperty("name").GetString()}");
            Console.WriteLine($"   Climate: {planet.GetProperty("climate").GetString()}");
            Console.WriteLine($"   Terrain: {planet.GetProperty("terrain").GetString()}");
            Console.WriteLine($"   Population: {planet.GetProperty("population").GetString()}");
        }
        
        // Query 6: Get starships
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 6: Get famous starships\n");
        var query6 = @"
{
  starships {
    name
    model
    starshipClass
    manufacturer
    length
    crew
    hyperdriveRating
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query6);
        Console.WriteLine("\nResult:");
        var result6 = db.Query(query6);
        var starships = result6.RootElement.GetProperty("data").GetProperty("starships");
        int sCount = 0;
        foreach (var starship in starships.EnumerateArray())
        {
            if (sCount++ >= 5) break;
            Console.WriteLine($"\n🚀 {starship.GetProperty("name").GetString()}");
            Console.WriteLine($"   Model: {starship.GetProperty("model").GetString()}");
            Console.WriteLine($"   Class: {starship.GetProperty("starshipClass").GetString()}");
            Console.WriteLine($"   Length: {starship.GetProperty("length").GetString()}m");
        }
        
        // Query 7: Create a new character (mutation)
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 7: Create a new character (Mutation)\n");
        var query7 = @"
mutation {
  createCharacter(input: {
    name: ""Rey""
    characterType: ""Human""
    appearsIn: [""NEWHOPE""]
    height: 170
    mass: 54
    hairColor: ""brown""
    eyeColor: ""hazel""
    birthYear: ""15ABY""
  }) {
    id
    name
    characterType
    height
  }
}";
        Console.WriteLine("Mutation:");
        Console.WriteLine(query7);
        Console.WriteLine("\nResult:");
        var result7 = db.Query(query7);
        Console.WriteLine(result7.RootElement.GetRawText());
        
        // Query 8: Query the newly created character
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 8: Verify Rey was created\n");
        var reyId = result7.RootElement.GetProperty("data").GetProperty("createCharacter").GetProperty("id").GetString();
        var query8 = $@"
{{
  character(id: ""{reyId}"") {{
    name
    characterType
    height
    hairColor
    eyeColor
    birthYear
  }}
}}";
        Console.WriteLine("Query:");
        Console.WriteLine(query8);
        Console.WriteLine("\nResult:");
        var result8 = db.Query(query8);
        Console.WriteLine(result8.RootElement.GetRawText());
        
        // Query 9: Get Darth Vader
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 9: Get Darth Vader details\n");
        var query9 = @"
{
  character(id: ""vader"") {
    name
    characterType
    height
    mass
    eyeColor
    birthYear
    appearsIn
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query9);
        Console.WriteLine("\nResult:");
        var result9 = db.Query(query9);
        Console.WriteLine(result9.RootElement.GetRawText());
        
        // Query 10: Get Han Solo with the Millennium Falcon
        Console.WriteLine("\n\n" + new string('-', 60));
        Console.WriteLine("📝 Query 10: Get Han Solo with his friends\n");
        var query10 = @"
{
  character(id: ""han"") {
    name
    characterType
    height
    mass
    hairColor
    eyeColor
    birthYear
    friends {
      name
      characterType
    }
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query10);
        Console.WriteLine("\nResult:");
        var result10 = db.Query(query10);
        Console.WriteLine(result10.RootElement.GetRawText());
        
        Console.WriteLine("\n\n" + new string('=', 60));
        Console.WriteLine("✅ STAR WARS DATABASE DEMO COMPLETE!");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("\n💡 Try your own queries! The database includes:");
        Console.WriteLine("   • 30+ Characters (Humans & Droids)");
        Console.WriteLine("   • 3 Films (Episodes IV, V, VI)");
        Console.WriteLine("   • 9 Planets");
        Console.WriteLine("   • 8 Species");
        Console.WriteLine("   • 7 Starships");
        Console.WriteLine("   • 5 Vehicles");
        Console.WriteLine("\n🌟 May the Force be with you! 🌟\n");
    }
}
