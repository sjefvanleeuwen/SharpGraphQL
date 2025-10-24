using System.Text.Json;
using SharpGraph.Core.GraphQL;
using SharpGraph.Core.Storage;

namespace SharpGraph.Examples;

/// <summary>
/// Comprehensive Star Wars database setup based on the official GraphQL Star Wars schema
/// Includes Characters (Humans and Droids), Films, Planets, Species, Starships, and Vehicles
/// </summary>
public class StarWarsDatabase
{
    private readonly string _dbPath;
    private readonly GraphQLExecutor _executor;
    
    // Tables
    private Table _characterTable = null!;
    private Table _filmTable = null!;
    private Table _planetTable = null!;
    private Table _speciesTable = null!;
    private Table _starshipTable = null!;
    private Table _vehicleTable = null!;
    
    public StarWarsDatabase(string dbPath = "starwars_db")
    {
        _dbPath = dbPath;
        Directory.CreateDirectory(dbPath);
        _executor = new GraphQLExecutor(dbPath);
        
        InitializeSchema();
        PopulateSampleData();
    }
    
    public GraphQLExecutor Executor => _executor;
    
    private Table CreateOrOpenTable(string name, string schema, List<ColumnDefinition> columns)
    {
        var tablePath = Path.Combine(_dbPath, $"{name}.tbl");
        Table table;
        
        if (File.Exists(tablePath))
        {
            table = Table.Open(name, _dbPath);
        }
        else
        {
            table = Table.Create(name, _dbPath, schema);
            table.SetSchema(schema, columns);
        }
        
        return table;
    }
    
    private void InitializeSchema()
    {
        // ==================== ENUM DEFINITIONS ====================
        
        // Episode Enum
        var episodeEnum = @"
enum Episode {
  NEWHOPE
  EMPIRE
  JEDI
}";
        
        // ==================== CHARACTER TABLE (Polymorphic: Human & Droid) ====================
        
        var characterSchema = @"
type Character {
  id: ID!
  name: String!
  appearsIn: [String]!
  friends: [Character]
  characterType: String!
  
  # Human-specific fields
  homePlanetId: ID
  homePlanet: Planet
  height: Float
  mass: Float
  hairColor: String
  skinColor: String
  eyeColor: String
  birthYear: String
  
  # Droid-specific fields
  primaryFunction: String
  
  # Relationships
  films: [Film]
  starships: [Starship]
  vehicles: [Vehicle]
}";
        
        var characterColumns = new List<ColumnDefinition>
        {
            new() { Name = "id", ScalarType = GraphQLScalarType.ID, IsNullable = false },
            new() { Name = "name", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "appearsIn", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = false },
            new() { Name = "friendIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "characterType", ScalarType = GraphQLScalarType.String, IsNullable = false }, // "Human" or "Droid"
            
            // Human fields
            new() { Name = "homePlanetId", ScalarType = GraphQLScalarType.ID, IsNullable = true },
            new() { Name = "height", ScalarType = GraphQLScalarType.Float, IsNullable = true },
            new() { Name = "mass", ScalarType = GraphQLScalarType.Float, IsNullable = true },
            new() { Name = "hairColor", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "skinColor", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "eyeColor", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "birthYear", ScalarType = GraphQLScalarType.String, IsNullable = true },
            
            // Droid fields
            new() { Name = "primaryFunction", ScalarType = GraphQLScalarType.String, IsNullable = true },
            
            // Film IDs for relationship
            new() { Name = "filmIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "starshipIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "vehicleIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            
            // Relationships (many-to-many via IDs stored above)
            new() 
            { 
                Name = "friends",
                IsList = true,
                RelatedTable = "Character",
                ForeignKey = "friendIds",
                RelationType = RelationType.ManyToMany
            },
            new() 
            { 
                Name = "homePlanet",
                IsList = false,
                RelatedTable = "Planet",
                ForeignKey = "homePlanetId",
                RelationType = RelationType.ManyToOne
            },
            new() 
            { 
                Name = "films",
                IsList = true,
                RelatedTable = "Film",
                ForeignKey = "filmIds",
                RelationType = RelationType.ManyToMany
            },
            new() 
            { 
                Name = "starships",
                IsList = true,
                RelatedTable = "Starship",
                ForeignKey = "starshipIds",
                RelationType = RelationType.ManyToMany
            },
            new() 
            { 
                Name = "vehicles",
                IsList = true,
                RelatedTable = "Vehicle",
                ForeignKey = "vehicleIds",
                RelationType = RelationType.ManyToMany
            }
        };
        
        _characterTable = CreateOrOpenTable("Character", characterSchema, characterColumns);
        _executor.RegisterTable("Character", _characterTable);
        
        // ==================== FILM TABLE ====================
        
        var filmSchema = @"
type Film {
  id: ID!
  title: String!
  episodeId: Int!
  openingCrawl: String!
  director: String!
  producer: String!
  releaseDate: String!
  
  # Relationships
  characters: [Character]
  planets: [Planet]
  starships: [Starship]
  vehicles: [Vehicle]
  species: [Species]
}";
        
        var filmColumns = new List<ColumnDefinition>
        {
            new() { Name = "id", ScalarType = GraphQLScalarType.ID, IsNullable = false },
            new() { Name = "title", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "episodeId", ScalarType = GraphQLScalarType.Int, IsNullable = false },
            new() { Name = "openingCrawl", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "director", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "producer", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "releaseDate", ScalarType = GraphQLScalarType.String, IsNullable = false },
            
            // Foreign key arrays
            new() { Name = "characterIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "planetIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "starshipIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "vehicleIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true },
            new() { Name = "speciesIds", ScalarType = GraphQLScalarType.String, IsList = true, IsNullable = true }
        };
        
        _filmTable = CreateOrOpenTable("Film", filmSchema, filmColumns);
        _executor.RegisterTable("Film", _filmTable);
        
        // ==================== PLANET TABLE ====================
        
        var planetSchema = @"
type Planet {
  id: ID!
  name: String!
  diameter: String
  rotationPeriod: String
  orbitalPeriod: String
  gravity: String
  population: String
  climate: String
  terrain: String
  surfaceWater: String
  
  # Relationships
  residents: [Character]
  films: [Film]
}";
        
        var planetColumns = new List<ColumnDefinition>
        {
            new() { Name = "id", ScalarType = GraphQLScalarType.ID, IsNullable = false },
            new() { Name = "name", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "diameter", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "rotationPeriod", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "orbitalPeriod", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "gravity", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "population", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "climate", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "terrain", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "surfaceWater", ScalarType = GraphQLScalarType.String, IsNullable = true }
        };
        
        _planetTable = CreateOrOpenTable("Planet", planetSchema, planetColumns);
        _executor.RegisterTable("Planet", _planetTable);
        
        // ==================== SPECIES TABLE ====================
        
        var speciesSchema = @"
type Species {
  id: ID!
  name: String!
  classification: String
  designation: String
  averageHeight: String
  averageLifespan: String
  eyeColors: String
  hairColors: String
  skinColors: String
  language: String
  homePlanetId: ID
  
  # Relationships
  homePlanet: Planet
  people: [Character]
  films: [Film]
}";
        
        var speciesColumns = new List<ColumnDefinition>
        {
            new() { Name = "id", ScalarType = GraphQLScalarType.ID, IsNullable = false },
            new() { Name = "name", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "classification", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "designation", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "averageHeight", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "averageLifespan", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "eyeColors", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "hairColors", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "skinColors", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "language", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "homePlanetId", ScalarType = GraphQLScalarType.ID, IsNullable = true },
            
            new() 
            { 
                Name = "homePlanet",
                IsList = false,
                RelatedTable = "Planet",
                ForeignKey = "homePlanetId",
                RelationType = RelationType.ManyToOne
            }
        };
        
        _speciesTable = CreateOrOpenTable("Species", speciesSchema, speciesColumns);
        _executor.RegisterTable("Species", _speciesTable);
        
        // ==================== STARSHIP TABLE ====================
        
        var starshipSchema = @"
type Starship {
  id: ID!
  name: String!
  model: String
  starshipClass: String
  manufacturer: String
  costInCredits: String
  length: String
  crew: String
  passengers: String
  maxAtmospheringSpeed: String
  hyperdriveRating: String
  MGLT: String
  cargoCapacity: String
  consumables: String
  
  # Relationships
  pilots: [Character]
  films: [Film]
}";
        
        var starshipColumns = new List<ColumnDefinition>
        {
            new() { Name = "id", ScalarType = GraphQLScalarType.ID, IsNullable = false },
            new() { Name = "name", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "model", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "starshipClass", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "manufacturer", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "costInCredits", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "length", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "crew", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "passengers", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "maxAtmospheringSpeed", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "hyperdriveRating", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "MGLT", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "cargoCapacity", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "consumables", ScalarType = GraphQLScalarType.String, IsNullable = true }
        };
        
        _starshipTable = CreateOrOpenTable("Starship", starshipSchema, starshipColumns);
        _executor.RegisterTable("Starship", _starshipTable);
        
        // ==================== VEHICLE TABLE ====================
        
        var vehicleSchema = @"
type Vehicle {
  id: ID!
  name: String!
  model: String
  vehicleClass: String
  manufacturer: String
  costInCredits: String
  length: String
  crew: String
  passengers: String
  maxAtmospheringSpeed: String
  cargoCapacity: String
  consumables: String
  
  # Relationships
  pilots: [Character]
  films: [Film]
}";
        
        var vehicleColumns = new List<ColumnDefinition>
        {
            new() { Name = "id", ScalarType = GraphQLScalarType.ID, IsNullable = false },
            new() { Name = "name", ScalarType = GraphQLScalarType.String, IsNullable = false },
            new() { Name = "model", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "vehicleClass", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "manufacturer", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "costInCredits", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "length", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "crew", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "passengers", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "maxAtmospheringSpeed", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "cargoCapacity", ScalarType = GraphQLScalarType.String, IsNullable = true },
            new() { Name = "consumables", ScalarType = GraphQLScalarType.String, IsNullable = true }
        };
        
        _vehicleTable = CreateOrOpenTable("Vehicle", vehicleSchema, vehicleColumns);
        _executor.RegisterTable("Vehicle", _vehicleTable);
    }
    
    private void PopulateSampleData()
    {
        // Check if data already exists
        if (_characterTable.SelectAll().Any())
        {
            Console.WriteLine("Star Wars database already populated.");
            return;
        }
        
        Console.WriteLine("Populating Star Wars database with sample data...");
        
        // ==================== PLANETS ====================
        
        CreatePlanet("tatooine", "Tatooine", "10465", "23", "304", "1 standard", "200000", "arid", "desert", "1");
        CreatePlanet("alderaan", "Alderaan", "12500", "24", "364", "1 standard", "2000000000", "temperate", "grasslands, mountains", "40");
        CreatePlanet("yavin4", "Yavin IV", "10200", "24", "4818", "1 standard", "1000", "temperate, tropical", "jungle, rainforests", "8");
        CreatePlanet("hoth", "Hoth", "7200", "23", "549", "1.1 standard", "unknown", "frozen", "tundra, ice caves, mountain ranges", "100");
        CreatePlanet("dagobah", "Dagobah", "8900", "23", "341", "N/A", "unknown", "murky", "swamp, jungles", "8");
        CreatePlanet("bespin", "Bespin", "118000", "12", "5110", "1.5 (surface), 1 standard (Cloud City)", "6000000", "temperate", "gas giant", "0");
        CreatePlanet("endor", "Endor", "4900", "18", "402", "0.85 standard", "30000000", "temperate", "forests, mountains, lakes", "8");
        CreatePlanet("naboo", "Naboo", "12120", "26", "312", "1 standard", "4500000000", "temperate", "grassy hills, swamps, forests, mountains", "12");
        CreatePlanet("coruscant", "Coruscant", "12240", "24", "368", "1 standard", "1000000000000", "temperate", "cityscape, mountains", "unknown");
        
        // ==================== SPECIES ====================
        
        CreateSpecies("human", "Human", "mammal", "sentient", "180", "120", "brown, blue, green, hazel, grey, amber", "blonde, brown, black, red", "caucasian, black, asian, hispanic", "Galactic Basic", "coruscant");
        CreateSpecies("droid", "Droid", "artificial", "sentient", "varies", "indefinite", "varies", "n/a", "n/a", "varies", null);
        CreateSpecies("wookiee", "Wookiee", "mammal", "sentient", "210", "400", "blue, green, yellow, brown, golden, red", "black, brown", "gray", "Shyriiwook", null);
        CreateSpecies("rodian", "Rodian", "sentient", "reptilian", "170", "unknown", "black", "n/a", "green, blue", "Galactic Basic", null);
        CreateSpecies("hutt", "Hutt", "gastropod", "sentient", "300", "1000", "yellow, red", "n/a", "green, brown, tan", "Huttese", null);
        CreateSpecies("yoda_species", "Yoda's species", "mammal", "sentient", "66", "900", "brown, green, yellow", "brown, white", "green, yellow", "Galactic basic", null);
        CreateSpecies("trandoshan", "Trandoshan", "reptile", "sentient", "200", "unknown", "yellow, orange", "none", "brown, green", "Dosh", null);
        CreateSpecies("ewok", "Ewok", "mammal", "sentient", "100", "unknown", "orange, brown", "white, brown, black", "brown", "Ewokese", "endor");
        
        // ==================== FILMS ====================
        
        CreateFilm("newhope", "A New Hope", 4, 
            "It is a period of civil war.\r\nRebel spaceships, striking\r\nfrom a hidden base, have won\r\ntheir first victory against\r\nthe evil Galactic Empire.\r\n\r\nDuring the battle, Rebel\r\nspies managed to steal secret\r\nplans to the Empire's\r\nultimate weapon, the DEATH\r\nSTAR, an armored space\r\nstation with enough power\r\nto destroy an entire planet.\r\n\r\nPursued by the Empire's\r\nsinister agents, Princess\r\nLeia races home aboard her\r\nstarship, custodian of the\r\nstolen plans that can save her\r\npeople and restore\r\nfreedom to the galaxy....",
            "George Lucas", "Gary Kurtz, Rick McCallum", "1977-05-25");
            
        CreateFilm("empire", "The Empire Strikes Back", 5,
            "It is a dark time for the\r\nRebellion. Although the Death\r\nStar has been destroyed,\r\nImperial troops have driven the\r\nRebel forces from their hidden\r\nbase and pursued them across\r\nthe galaxy.\r\n\r\nEvading the dreaded Imperial\r\nStarfleet, a group of freedom\r\nfighters led by Luke Skywalker\r\nhas established a new secret\r\nbase on the remote ice world\r\nof Hoth.\r\n\r\nThe evil lord Darth Vader,\r\nobsessed with finding young\r\nSkywalker, has dispatched\r\nthousands of remote probes into\r\nthe far reaches of space....",
            "Irvin Kershner", "Gary Kurtz, Rick McCallum", "1980-05-17");
            
        CreateFilm("jedi", "Return of the Jedi", 6,
            "Luke Skywalker has returned to\r\nhis home planet of Tatooine in\r\nan attempt to rescue his\r\nfriend Han Solo from the\r\nclutches of the vile gangster\r\nJabba the Hutt.\r\n\r\nLittle does Luke know that the\r\nGALACTIC EMPIRE has secretly\r\nbegun construction on a new\r\narmored space station even\r\nmore powerful than the first\r\ndreaded Death Star.\r\n\r\nWhen completed, this ultimate\r\nweapon will spell certain doom\r\nfor the small band of rebels\r\nstruggling to restore freedom\r\nto the galaxy...",
            "Richard Marquand", "Howard G. Kazanjian, George Lucas, Rick McCallum", "1983-05-25");
        
        // ==================== STARSHIPS ====================
        
        CreateStarship("xwing", "X-wing", "T-65 X-wing", "Starfighter", "Incom Corporation", "149999", "12.5", "1", "0", "1050", "1.0", "100", "110", "1 week");
        CreateStarship("ywing", "Y-wing", "BTL Y-wing", "assault starfighter", "Koensayr Manufacturing", "134999", "14", "2", "0", "1000km", "1.0", "80", "110", "1 week");
        CreateStarship("millennium_falcon", "Millennium Falcon", "YT-1300 light freighter", "Light freighter", "Corellian Engineering Corporation", "100000", "34.37", "4", "6", "1050", "0.5", "75", "100000", "2 months");
        CreateStarship("tie_fighter", "TIE Advanced x1", "Twin Ion Engine Advanced x1", "Starfighter", "Sienar Fleet Systems", "unknown", "9.2", "1", "0", "1200", "1.0", "105", "150", "5 days");
        CreateStarship("slave1", "Slave 1", "Firespray-31-class patrol and attack", "Patrol craft", "Kuat Systems Engineering", "unknown", "21.5", "1", "6", "1000", "3.0", "70", "70000", "1 month");
        CreateStarship("imperial_shuttle", "Imperial shuttle", "Lambda-class T-4a shuttle", "Armed government transport", "Sienar Fleet Systems", "240000", "20", "6", "20", "850", "1.0", "50", "80000", "2 months");
        CreateStarship("death_star", "Death Star", "DS-1 Orbital Battle Station", "Deep Space Mobile Battlestation", "Imperial Department of Military Research, Sienar Fleet Systems", "1000000000000", "120000", "342953", "843342", "n/a", "4.0", "10", "1000000000000", "3 years");
        
        // ==================== VEHICLES ====================
        
        CreateVehicle("sand_crawler", "Sand Crawler", "Digger Crawler", "wheeled", "Corellia Mining Corporation", "150000", "36.8", "46", "30", "30", "50000", "2 months");
        CreateVehicle("t16_skyhopper", "T-16 skyhopper", "T-16 skyhopper", "repulsorcraft", "Incom Corporation", "14500", "10.4", "1", "1", "1200", "50", "0");
        CreateVehicle("speeder_bike", "Speeder bike", "74-Z speeder bike", "speeder", "Aratech Repulsor Company", "8000", "3", "1", "1", "360", "4", "1 day");
        CreateVehicle("atat", "AT-AT", "All Terrain Armored Transport", "assault walker", "Kuat Drive Yards, Imperial Department of Military Research", "unknown", "20", "5", "40", "60", "1000", "unknown");
        CreateVehicle("atst", "AT-ST", "All Terrain Scout Transport", "walker", "Kuat Drive Yards, Imperial Department of Military Research", "unknown", "2", "2", "0", "90", "200", "none");
        
        // ==================== CHARACTERS ====================
        
        // Luke Skywalker
        CreateCharacter("luke", "Luke Skywalker", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 172, mass: 77, hairColor: "blond", skinColor: "fair", eyeColor: "blue", birthYear: "19BBY",
            friendIds: new[] { "han", "leia", "c3po", "r2d2" },
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "xwing", "imperial_shuttle" },
            vehicleIds: new[] { "speeder_bike", "imperial_shuttle" });
        
        // C-3PO
        CreateCharacter("c3po", "C-3PO", "Droid", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            primaryFunction: "Protocol",
            height: 167, mass: 75, eyeColor: "yellow",
            friendIds: new[] { "luke", "han", "leia", "r2d2", "chewbacca" },
            filmIds: new[] { "newhope", "empire", "jedi" });
        
        // R2-D2
        CreateCharacter("r2d2", "R2-D2", "Droid", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            primaryFunction: "Astromech",
            height: 96, mass: 32, eyeColor: "red",
            friendIds: new[] { "luke", "han", "leia", "c3po" },
            filmIds: new[] { "newhope", "empire", "jedi" });
        
        // Darth Vader
        CreateCharacter("vader", "Darth Vader", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 202, mass: 136, skinColor: "white", eyeColor: "yellow", birthYear: "41.9BBY",
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "tie_fighter" });
        
        // Leia Organa
        CreateCharacter("leia", "Leia Organa", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "alderaan", height: 150, mass: 49, hairColor: "brown", skinColor: "light", eyeColor: "brown", birthYear: "19BBY",
            friendIds: new[] { "luke", "han", "c3po", "r2d2", "chewbacca" },
            filmIds: new[] { "newhope", "empire", "jedi" },
            vehicleIds: new[] { "speeder_bike" });
        
        // Owen Lars
        CreateCharacter("owen", "Owen Lars", "Human", new[] { "NEWHOPE" },
            homePlanetId: "tatooine", height: 178, mass: 120, hairColor: "brown, grey", skinColor: "light", eyeColor: "blue", birthYear: "52BBY",
            filmIds: new[] { "newhope" });
        
        // Beru Whitesun Lars
        CreateCharacter("beru", "Beru Whitesun lars", "Human", new[] { "NEWHOPE" },
            homePlanetId: "tatooine", height: 165, mass: 75, hairColor: "brown", skinColor: "light", eyeColor: "blue", birthYear: "47BBY",
            filmIds: new[] { "newhope" });
        
        // R5-D4
        CreateCharacter("r5d4", "R5-D4", "Droid", new[] { "NEWHOPE" },
            primaryFunction: "Astromech",
            height: 97, mass: 32, eyeColor: "red",
            filmIds: new[] { "newhope" });
        
        // Biggs Darklighter
        CreateCharacter("biggs", "Biggs Darklighter", "Human", new[] { "NEWHOPE" },
            homePlanetId: "tatooine", height: 183, mass: 84, hairColor: "black", skinColor: "light", eyeColor: "brown", birthYear: "24BBY",
            filmIds: new[] { "newhope" },
            starshipIds: new[] { "xwing" });
        
        // Obi-Wan Kenobi
        CreateCharacter("obiwan", "Obi-Wan Kenobi", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "stewjon", height: 182, mass: 77, hairColor: "auburn, white", skinColor: "fair", eyeColor: "blue-gray", birthYear: "57BBY",
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "jedi_starfighter", "jedi_interceptor", "belbullab" });
        
        // Anakin Skywalker (same as Vader, but different record for young Anakin)
        CreateCharacter("anakin", "Anakin Skywalker", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 188, mass: 84, hairColor: "blond", skinColor: "fair", eyeColor: "blue", birthYear: "41.9BBY",
            filmIds: new[] { "newhope", "empire", "jedi" });
        
        // Wilhuff Tarkin
        CreateCharacter("tarkin", "Wilhuff Tarkin", "Human", new[] { "NEWHOPE" },
            height: 180, mass: null, hairColor: "auburn, grey", skinColor: "fair", eyeColor: "blue", birthYear: "64BBY",
            filmIds: new[] { "newhope" });
        
        // Chewbacca
        CreateCharacter("chewbacca", "Chewbacca", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 228, mass: 112, hairColor: "brown", eyeColor: "blue", birthYear: "200BBY",
            friendIds: new[] { "luke", "han", "leia", "r2d2", "c3po" },
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "millennium_falcon", "imperial_shuttle" });
        
        // Han Solo
        CreateCharacter("han", "Han Solo", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 180, mass: 80, hairColor: "brown", skinColor: "fair", eyeColor: "brown", birthYear: "29BBY",
            friendIds: new[] { "luke", "leia", "r2d2", "chewbacca" },
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "millennium_falcon", "imperial_shuttle" });
        
        // Greedo
        CreateCharacter("greedo", "Greedo", "Human", new[] { "NEWHOPE" },
            height: 173, mass: 74, eyeColor: "black", birthYear: "44BBY",
            filmIds: new[] { "newhope" });
        
        // Jabba Desilijic Tiure
        CreateCharacter("jabba", "Jabba Desilijic Tiure", "Human", new[] { "NEWHOPE", "JEDI" },
            height: 175, mass: 1358, eyeColor: "orange", birthYear: "600BBY",
            filmIds: new[] { "newhope", "jedi" });
        
        // Wedge Antilles
        CreateCharacter("wedge", "Wedge Antilles", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 170, mass: 77, hairColor: "brown", skinColor: "fair", eyeColor: "hazel", birthYear: "21BBY",
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "xwing" });
        
        // Jek Tono Porkins
        CreateCharacter("porkins", "Jek Tono Porkins", "Human", new[] { "NEWHOPE" },
            height: 180, mass: 110, hairColor: "brown", skinColor: "fair", eyeColor: "blue", birthYear: "unknown",
            filmIds: new[] { "newhope" },
            starshipIds: new[] { "xwing" });
        
        // Yoda
        CreateCharacter("yoda", "Yoda", "Human", new[] { "EMPIRE", "JEDI" },
            height: 66, mass: 17, hairColor: "white", skinColor: "green", eyeColor: "brown", birthYear: "896BBY",
            filmIds: new[] { "empire", "jedi" });
        
        // Palpatine (Emperor)
        CreateCharacter("palpatine", "Palpatine", "Human", new[] { "EMPIRE", "JEDI" },
            height: 170, mass: 75, hairColor: "grey", skinColor: "pale", eyeColor: "yellow", birthYear: "82BBY",
            filmIds: new[] { "empire", "jedi" });
        
        // Boba Fett
        CreateCharacter("boba", "Boba Fett", "Human", new[] { "EMPIRE", "JEDI" },
            homePlanetId: "kamino", height: 183, mass: 78, hairColor: "black", skinColor: "fair", eyeColor: "brown", birthYear: "31.5BBY",
            filmIds: new[] { "empire", "jedi" },
            starshipIds: new[] { "slave1" });
        
        // IG-88
        CreateCharacter("ig88", "IG-88", "Droid", new[] { "EMPIRE" },
            primaryFunction: "Assassin",
            height: 200, mass: 140, eyeColor: "red",
            filmIds: new[] { "empire" });
        
        // Bossk
        CreateCharacter("bossk", "Bossk", "Human", new[] { "EMPIRE" },
            height: 190, mass: 113, eyeColor: "red", birthYear: "53BBY",
            filmIds: new[] { "empire" });
        
        // Lando Calrissian
        CreateCharacter("lando", "Lando Calrissian", "Human", new[] { "EMPIRE", "JEDI" },
            height: 177, mass: 79, hairColor: "black", skinColor: "dark", eyeColor: "brown", birthYear: "31BBY",
            filmIds: new[] { "empire", "jedi" },
            starshipIds: new[] { "millennium_falcon" });
        
        // Lobot
        CreateCharacter("lobot", "Lobot", "Human", new[] { "EMPIRE" },
            height: 175, mass: 79, hairColor: "none", skinColor: "light", eyeColor: "blue", birthYear: "37BBY",
            filmIds: new[] { "empire" });
        
        // Ackbar
        CreateCharacter("ackbar", "Ackbar", "Human", new[] { "JEDI" },
            height: 180, mass: 83, eyeColor: "orange", birthYear: "41BBY",
            filmIds: new[] { "jedi" });
        
        // Mon Mothma
        CreateCharacter("mothma", "Mon Mothma", "Human", new[] { "JEDI" },
            height: 150, mass: null, hairColor: "auburn", skinColor: "fair", eyeColor: "blue", birthYear: "48BBY",
            filmIds: new[] { "jedi" });
        
        // Arvel Crynyd
        CreateCharacter("arvel", "Arvel Crynyd", "Human", new[] { "JEDI" },
            height: null, mass: null, hairColor: "brown", skinColor: "fair", eyeColor: "brown", birthYear: "unknown",
            filmIds: new[] { "jedi" },
            starshipIds: new[] { "awing" });
        
        // Wicket Systri Warrick
        CreateCharacter("wicket", "Wicket Systri Warrick", "Human", new[] { "JEDI" },
            height: 88, mass: 20, hairColor: "brown", skinColor: "brown", eyeColor: "brown", birthYear: "8BBY",
            filmIds: new[] { "jedi" });
        
        // Nien Nunb
        CreateCharacter("nien", "Nien Nunb", "Human", new[] { "JEDI" },
            height: 160, mass: 68, eyeColor: "black", birthYear: "unknown",
            filmIds: new[] { "jedi" },
            starshipIds: new[] { "millennium_falcon" });
        
        // Qui-Gon Jinn
        CreateCharacter("quigon", "Qui-Gon Jinn", "Human", new[] { "NEWHOPE" },
            height: 193, mass: 89, hairColor: "brown", skinColor: "fair", eyeColor: "blue", birthYear: "92BBY",
            filmIds: new[] { "newhope" });
        
        // Nute Gunray
        CreateCharacter("nute", "Nute Gunray", "Human", new[] { "NEWHOPE" },
            height: 191, mass: 90, eyeColor: "red", birthYear: "unknown",
            filmIds: new[] { "newhope" });
        
        Console.WriteLine($"✅ Created {_characterTable.SelectAll().Count()} characters");
        Console.WriteLine($"✅ Created {_filmTable.SelectAll().Count()} films");
        Console.WriteLine($"✅ Created {_planetTable.SelectAll().Count()} planets");
        Console.WriteLine($"✅ Created {_speciesTable.SelectAll().Count()} species");
        Console.WriteLine($"✅ Created {_starshipTable.SelectAll().Count()} starships");
        Console.WriteLine($"✅ Created {_vehicleTable.SelectAll().Count()} vehicles");
        Console.WriteLine("\n🌟 Star Wars database ready!");
    }
    
    // Helper methods for creating records
    
    private void CreatePlanet(string id, string name, string diameter, string rotationPeriod, 
        string orbitalPeriod, string gravity, string population, string climate, string terrain, string surfaceWater)
    {
        var planet = new
        {
            id,
            name,
            diameter,
            rotationPeriod,
            orbitalPeriod,
            gravity,
            population,
            climate,
            terrain,
            surfaceWater
        };
        _planetTable.Insert(id, JsonSerializer.Serialize(planet));
    }
    
    private void CreateSpecies(string id, string name, string classification, string designation,
        string averageHeight, string averageLifespan, string eyeColors, string hairColors, 
        string skinColors, string language, string? homePlanetId)
    {
        var species = new
        {
            id,
            name,
            classification,
            designation,
            averageHeight,
            averageLifespan,
            eyeColors,
            hairColors,
            skinColors,
            language,
            homePlanetId
        };
        _speciesTable.Insert(id, JsonSerializer.Serialize(species));
    }
    
    private void CreateFilm(string id, string title, int episodeId, string openingCrawl,
        string director, string producer, string releaseDate)
    {
        var film = new
        {
            id,
            title,
            episodeId,
            openingCrawl,
            director,
            producer,
            releaseDate,
            characterIds = Array.Empty<string>(),
            planetIds = Array.Empty<string>(),
            starshipIds = Array.Empty<string>(),
            vehicleIds = Array.Empty<string>(),
            speciesIds = Array.Empty<string>()
        };
        _filmTable.Insert(id, JsonSerializer.Serialize(film));
    }
    
    private void CreateStarship(string id, string name, string model, string starshipClass,
        string manufacturer, string costInCredits, string length, string crew, string passengers,
        string maxAtmospheringSpeed, string hyperdriveRating, string mglt, string cargoCapacity, string consumables)
    {
        var starship = new
        {
            id,
            name,
            model,
            starshipClass,
            manufacturer,
            costInCredits,
            length,
            crew,
            passengers,
            maxAtmospheringSpeed,
            hyperdriveRating,
            MGLT = mglt,
            cargoCapacity,
            consumables
        };
        _starshipTable.Insert(id, JsonSerializer.Serialize(starship));
    }
    
    private void CreateVehicle(string id, string name, string model, string vehicleClass,
        string manufacturer, string costInCredits, string length, string crew, string passengers,
        string maxAtmospheringSpeed, string cargoCapacity, string consumables)
    {
        var vehicle = new
        {
            id,
            name,
            model,
            vehicleClass,
            manufacturer,
            costInCredits,
            length,
            crew,
            passengers,
            maxAtmospheringSpeed,
            cargoCapacity,
            consumables
        };
        _vehicleTable.Insert(id, JsonSerializer.Serialize(vehicle));
    }
    
    private void CreateCharacter(string id, string name, string characterType, string[] appearsIn,
        string? homePlanetId = null, float? height = null, float? mass = null, string? hairColor = null,
        string? skinColor = null, string? eyeColor = null, string? birthYear = null, string? primaryFunction = null,
        string[]? friendIds = null, string[]? filmIds = null, string[]? starshipIds = null, string[]? vehicleIds = null)
    {
        var character = new
        {
            id,
            name,
            characterType,
            appearsIn,
            homePlanetId,
            height,
            mass,
            hairColor,
            skinColor,
            eyeColor,
            birthYear,
            primaryFunction,
            friendIds = friendIds ?? Array.Empty<string>(),
            filmIds = filmIds ?? Array.Empty<string>(),
            starshipIds = starshipIds ?? Array.Empty<string>(),
            vehicleIds = vehicleIds ?? Array.Empty<string>()
        };
        _characterTable.Insert(id, JsonSerializer.Serialize(character));
    }
    
    public JsonDocument Query(string query)
    {
        return _executor.Execute(query);
    }
}
