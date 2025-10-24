using System.Text.Json;

namespace SharpGraph.Examples;

/// <summary>
/// Star Wars seed data generator
/// Generates seed_data.json with all Star Wars entities
/// </summary>
public class StarWarsDataGenerator
{
    // In-memory data collections
    private readonly List<object> _characters = new();
    private readonly List<object> _films = new();
    private readonly List<object> _planets = new();
    private readonly List<object> _species = new();
    private readonly List<object> _starships = new();
    private readonly List<object> _vehicles = new();
    
    public StarWarsDataGenerator()
    {
        GenerateData();
    }
    
    private void GenerateData()
    {
        Console.WriteLine("🌌 Generating Star Wars universe data...\n");
        
        // Generate in order: Planets, Species, Films, Starships, Vehicles, then Characters
        GeneratePlanets();
        GenerateSpecies();
        GenerateFilms();
        GenerateStarships();
        GenerateVehicles();
        GenerateCharacters();
        
        Console.WriteLine($"✅ Generated:");
        Console.WriteLine($"   • {_planets.Count} planets");
        Console.WriteLine($"   • {_species.Count} species");
        Console.WriteLine($"   • {_films.Count} films");
        Console.WriteLine($"   • {_starships.Count} starships");
        Console.WriteLine($"   • {_vehicles.Count} vehicles");
        Console.WriteLine($"   • {_characters.Count} characters\n");
    }
    
    public void ExportToJson(string filePath)
    {
        var data = new
        {
            Planet = _planets,
            Species = _species,
            Film = _films,
            Starship = _starships,
            Vehicle = _vehicles,
            Character = _characters
        };
        
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(filePath, json);
        
        Console.WriteLine($"💾 Exported seed data to: {filePath}");
    }
    
    private void GeneratePlanets()
    {
        AddPlanet("tatooine", "Tatooine", "10465", "23", "304", "1 standard", "200000", "arid", "desert", "1");
        AddPlanet("alderaan", "Alderaan", "12500", "24", "364", "1 standard", "2000000000", "temperate", "grasslands, mountains", "40");
        AddPlanet("yavin", "Yavin IV", "10200", "24", "4818", "1 standard", "1000", "temperate, tropical", "jungle, rainforests", "8");
        AddPlanet("hoth", "Hoth", "7200", "23", "549", "1.1 standard", "unknown", "frozen", "tundra, ice caves, mountain ranges", "100");
        AddPlanet("dagobah", "Dagobah", "8900", "23", "341", "N/A", "unknown", "murky", "swamp, jungles", "8");
        AddPlanet("bespin", "Bespin", "118000", "12", "5110", "1.5 (Cloud City)", "6000000", "temperate", "gas giant", "0");
        AddPlanet("endor", "Endor", "4900", "18", "402", "0.85 standard", "30000000", "temperate", "forests, mountains, lakes", "8");
        AddPlanet("naboo", "Naboo", "12120", "26", "312", "1 standard", "4500000000", "temperate", "grassy hills, swamps, forests, mountains", "12");
        AddPlanet("coruscant", "Coruscant", "12240", "24", "368", "1 standard", "1000000000000", "temperate", "cityscape, mountains", "unknown");
        AddPlanet("kamino", "Kamino", "19720", "27", "463", "1 standard", "1000000000", "temperate", "ocean", "100");
    }
    
    private void GenerateSpecies()
    {
        AddSpecies("human", "Human", "mammal", "sentient", "180", "120", "brown, blue, green, hazel, grey, amber", "blonde, brown, black, red, white, grey", "caucasian, black, asian, hispanic", "Galactic Basic", "coruscant");
        AddSpecies("droid", "Droid", "artificial", "sentient", "varies", "indefinite", "varies", "n/a", "varies", "varies", null);
        AddSpecies("wookie", "Wookiee", "mammal", "sentient", "210", "400", "blue, green, yellow, brown, golden, red", "black, brown", "gray", "Shyriiwook", "kashyyyk");
        AddSpecies("rodian", "Rodian", "sentient", "reptilian", "170", "unknown", "black", "n/a", "green, blue", "Galactic Basic", "rodia");
        AddSpecies("hutt", "Hutt", "gastropod", "sentient", "300", "1000", "yellow, red", "n/a", "green, brown, tan", "Huttese", "nal_hutta");
        AddSpecies("yoda_species", "Yoda's species", "mammal", "sentient", "66", "900", "brown, green, yellow", "brown, white", "green, yellow", "Galactic basic", null);
        AddSpecies("trandoshan", "Trandoshan", "reptile", "sentient", "200", "unknown", "yellow, orange", "none", "brown, green", "Dosh", "trandosha");
        AddSpecies("mon_calamari", "Mon Calamari", "amphibian", "sentient", "160", "unknown", "yellow", "none", "red, blue, brown, magenta", "Mon Calamarian", "mon_cala");
        AddSpecies("ewok", "Ewok", "mammal", "sentient", "100", "unknown", "orange, brown", "white, brown, black", "brown", "Ewokese", "endor");
        AddSpecies("sullustan", "Sullustan", "mammal", "sentient", "180", "unknown", "black", "none", "pale", "Sullutese", "sullust");
    }
    
    private void GenerateFilms()
    {
        // Prequel Trilogy
        AddFilm("phantom", "The Phantom Menace", 1,
            "Turmoil has engulfed the Galactic Republic. The taxation of trade routes to outlying star systems is in dispute.\r\n\r\nHoping to resolve the matter with a blockade of deadly battleships, the greedy Trade Federation has stopped all shipping to the small planet of Naboo.\r\n\r\nWhile the Congress of the Republic endlessly debates this alarming chain of events, the Supreme Chancellor has secretly dispatched two Jedi Knights, the guardians of peace and justice in the galaxy, to settle the conflict....",
            "George Lucas", "Rick McCallum", "1999-05-19");
            
        AddFilm("clones", "Attack of the Clones", 2,
            "There is unrest in the Galactic Senate. Several thousand solar systems have declared their intentions to leave the Republic.\r\n\r\nThis separatist movement, under the leadership of the mysterious Count Dooku, has made it difficult for the limited number of Jedi Knights to maintain peace and order in the galaxy.\r\n\r\nSenator Amidala, the former Queen of Naboo, is returning to the Galactic Senate to vote on the critical issue of creating an ARMY OF THE REPUBLIC to assist the overwhelmed Jedi....",
            "George Lucas", "Rick McCallum", "2002-05-16");
            
        AddFilm("sith", "Revenge of the Sith", 3,
            "War! The Republic is crumbling under attacks by the ruthless Sith Lord, Count Dooku. There are heroes on both sides. Evil is everywhere.\r\n\r\nIn a stunning move, the fiendish droid leader, General Grievous, has swept into the Republic capital and kidnapped Chancellor Palpatine, leader of the Galactic Senate.\r\n\r\nAs the Separatist Droid Army attempts to flee the besieged capital with their valuable hostage, two Jedi Knights lead a desperate mission to rescue the captive Chancellor....",
            "George Lucas", "Rick McCallum", "2005-05-19");
        
        // Original Trilogy
        AddFilm("newhope", "A New Hope", 4,
            "It is a period of civil war. Rebel spaceships, striking from a hidden base, have won their first victory against the evil Galactic Empire.\r\n\r\nDuring the battle, Rebel spies managed to steal secret plans to the Empire's ultimate weapon, the DEATH STAR, an armored space station with enough power to destroy an entire planet.\r\n\r\nPursued by the Empire's sinister agents, Princess Leia races home aboard her starship, custodian of the stolen plans that can save her people and restore freedom to the galaxy....",
            "George Lucas", "Gary Kurtz, Rick McCallum", "1977-05-25");
            
        AddFilm("empire", "The Empire Strikes Back", 5,
            "It is a dark time for the Rebellion. Although the Death Star has been destroyed, Imperial troops have driven the Rebel forces from their hidden base and pursued them across the galaxy.\r\n\r\nEvading the dreaded Imperial Starfleet, a group of freedom fighters led by Luke Skywalker has established a new secret base on the remote ice world of Hoth.\r\n\r\nThe evil lord Darth Vader, obsessed with finding young Skywalker, has dispatched thousands of remote probes into the far reaches of space....",
            "Irvin Kershner", "Gary Kurtz, Rick McCallum", "1980-05-17");
            
        AddFilm("jedi", "Return of the Jedi", 6,
            "Luke Skywalker has returned to his home planet of Tatooine in an attempt to rescue his friend Han Solo from the clutches of the vile gangster Jabba the Hutt.\r\n\r\nLittle does Luke know that the GALACTIC EMPIRE has secretly begun construction on a new armored space station even more powerful than the first dreaded Death Star.\r\n\r\nWhen completed, this ultimate weapon will spell certain doom for the small band of rebels struggling to restore freedom to the galaxy....",
            "Richard Marquand", "Howard G. Kazanjian, George Lucas, Rick McCallum", "1983-05-25");
        
        // Sequel Trilogy
        AddFilm("awakens", "The Force Awakens", 7,
            "Luke Skywalker has vanished. In his absence, the sinister FIRST ORDER has risen from the ashes of the Empire and will not rest until Skywalker, the last Jedi, has been destroyed.\r\n\r\nWith the support of the REPUBLIC, General Leia Organa leads a brave RESISTANCE. She is desperate to find her brother Luke and gain his help in restoring peace and justice to the galaxy.\r\n\r\nLeia has sent her most daring pilot on a secret mission to Jakku, where an old ally has discovered a clue to Luke's whereabouts....",
            "J.J. Abrams", "Kathleen Kennedy, J.J. Abrams, Bryan Burk", "2015-12-18");
            
        AddFilm("lastjedi", "The Last Jedi", 8,
            "The FIRST ORDER reigns. Having decimated the peaceful Republic, Supreme Leader Snoke now deploys the merciless legions to seize military control of the galaxy.\r\n\r\nOnly General Leia Organa's band of RESISTANCE fighters stand against the rising tyranny, certain that Jedi Master Luke Skywalker will return and restore a spark of hope to the fight.\r\n\r\nBut the Resistance has been exposed. As the First Order speeds toward the rebel base, the brave heroes mount a desperate escape....",
            "Rian Johnson", "Kathleen Kennedy, Ram Bergman", "2017-12-15");
            
        AddFilm("skywalker", "The Rise of Skywalker", 9,
            "The dead speak! The galaxy has heard a mysterious broadcast, a threat of REVENGE in the sinister voice of the late EMPEROR PALPATINE.\r\n\r\nGENERAL LEIA ORGANA dispatches secret agents to gather intelligence, while REY, the last hope of the Jedi, trains for battle against the diabolical FIRST ORDER.\r\n\r\nMeanwhile, Supreme Leader KYLO REN rages in search of the phantom Emperor, determined to destroy any threat to his power....",
            "J.J. Abrams", "Kathleen Kennedy, J.J. Abrams, Michelle Rejwan", "2019-12-20");
    }
    
    private void GenerateStarships()
    {
        AddStarship("xwing", "X-wing", "T-65 X-wing", "Starfighter", "Incom Corporation", "149999", "12.5", "1", "0", "1050", "1.0", "100", "110", "1 week");
        AddStarship("ywing", "Y-wing", "BTL Y-wing", "assault starfighter", "Koensayr Manufacturing", "134999", "14", "2", "0", "1000km", "1.0", "80", "110", "1 week");
        AddStarship("millennium_falcon", "Millennium Falcon", "YT-1300 light freighter", "Light freighter", "Corellian Engineering Corporation", "100000", "34.37", "4", "6", "1050", "0.5", "75", "100000", "2 months");
        AddStarship("tie_fighter", "TIE Advanced x1", "Twin Ion Engine Advanced x1", "Starfighter", "Sienar Fleet Systems", "unknown", "9.2", "1", "0", "1200", "1.0", "105", "150", "5 days");
        AddStarship("slave1", "Slave 1", "Firespray-31-class patrol and attack", "Patrol craft", "Kuat Systems Engineering", "unknown", "21.5", "1", "6", "1000", "3.0", "70", "70000", "1 month");
        AddStarship("imperial_shuttle", "Imperial shuttle", "Lambda-class T-4a shuttle", "Armed government transport", "Sienar Fleet Systems", "240000", "20", "6", "20", "850", "1.0", "50", "80000", "2 months");
        AddStarship("death_star", "Death Star", "DS-1 Orbital Battle Station", "Deep Space Mobile Battlestation", "Imperial Department of Military Research, Sienar Fleet Systems", "1000000000000", "120000", "342953", "843342", "n/a", "4.0", "10", "1000000000000", "3 years");
        AddStarship("awing", "A-wing", "RZ-1 A-wing Interceptor", "Starfighter", "Alliance Underground Engineering, Incom Corporation", "175000", "9.6", "1", "0", "1300", "1.0", "120", "40", "1 week");
    }
    
    private void GenerateVehicles()
    {
        AddVehicle("sand_crawler", "Sand Crawler", "Digger Crawler", "wheeled", "Corellia Mining Corporation", "150000", "36.8", "46", "30", "30", "50000", "2 months");
        AddVehicle("t16_skyhopper", "T-16 skyhopper", "T-16 skyhopper", "repulsorcraft", "Incom Corporation", "14500", "10.4", "1", "1", "1200", "50", "0");
        AddVehicle("speeder_bike", "Speeder bike", "74-Z speeder bike", "speeder", "Aratech Repulsor Company", "8000", "3", "1", "1", "360", "4", "1 day");
        AddVehicle("atat", "AT-AT", "All Terrain Armored Transport", "assault walker", "Kuat Drive Yards, Imperial Department of Military Research", "unknown", "20", "5", "40", "60", "1000", "unknown");
        AddVehicle("atst", "AT-ST", "All Terrain Scout Transport", "walker", "Kuat Drive Yards, Imperial Department of Military Research", "unknown", "2", "2", "0", "90", "200", "none");
        AddVehicle("snowspeeder", "Snowspeeder", "t-47 airspeeder", "airspeeder", "Incom corporation", "unknown", "4.5", "2", "0", "650", "10", "none");
    }
    
    private void GenerateCharacters()
    {
        // Original Trilogy - Main Characters
        AddCharacter("luke", "Luke Skywalker", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 172, mass: 77, hairColor: "blond", skinColor: "fair", eyeColor: "blue", birthYear: "19BBY",
            friendIds: new[] { "han", "leia", "c3po", "r2d2" },
            filmIds: new[] { "newhope", "empire", "jedi", "awakens", "lastjedi", "skywalker" },
            starshipIds: new[] { "xwing", "imperial_shuttle" },
            vehicleIds: new[] { "snowspeeder", "speeder_bike" });

        AddCharacter("c3po", "C-3PO", "Droid", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            primaryFunction: "Protocol",
            height: 167, mass: 75, eyeColor: "yellow", birthYear: "112BBY",
            friendIds: new[] { "luke", "han", "leia", "r2d2", "chewbacca" },
            filmIds: new[] { "phantom", "clones", "sith", "newhope", "empire", "jedi" });

        AddCharacter("r2d2", "R2-D2", "Droid", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            primaryFunction: "Astromech",
            height: 96, mass: 32, eyeColor: "red", birthYear: "33BBY",
            friendIds: new[] { "luke", "han", "leia", "c3po" },
            filmIds: new[] { "phantom", "clones", "sith", "newhope", "empire", "jedi", "awakens" },
            starshipIds: new[] { "xwing" });

        AddCharacter("vader", "Darth Vader", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 202, mass: 136, hairColor: "none", skinColor: "white", eyeColor: "yellow", birthYear: "41.9BBY",
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "tie_fighter" });

        AddCharacter("leia", "Leia Organa", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "alderaan", height: 150, mass: 49, hairColor: "brown", skinColor: "light", eyeColor: "brown", birthYear: "19BBY",
            friendIds: new[] { "luke", "han", "chewbacca", "r2d2" },
            filmIds: new[] { "newhope", "empire", "jedi", "awakens", "lastjedi", "skywalker" });

        AddCharacter("owen", "Owen Lars", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 178, mass: 120, hairColor: "brown, grey", skinColor: "light", eyeColor: "blue", birthYear: "52BBY",
            filmIds: new[] { "newhope", "clones", "sith" });

        AddCharacter("beru", "Beru Whitesun lars", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 165, mass: 75, hairColor: "brown", skinColor: "light", eyeColor: "blue", birthYear: "47BBY",
            filmIds: new[] { "newhope", "clones", "sith" });

        AddCharacter("r5d4", "R5-D4", "Droid", new[] { "NEWHOPE" },
            primaryFunction: "Astromech",
            height: 97, mass: 32, eyeColor: "red", birthYear: "unknown",
            filmIds: new[] { "newhope" });

        AddCharacter("biggs", "Biggs Darklighter", "Human", new[] { "NEWHOPE" },
            homePlanetId: "tatooine", height: 183, mass: 84, hairColor: "black", skinColor: "light", eyeColor: "brown", birthYear: "24BBY",
            filmIds: new[] { "newhope" },
            starshipIds: new[] { "xwing" });

        AddCharacter("obi-wan", "Obi-Wan Kenobi", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "stewjon", height: 182, mass: 77, hairColor: "auburn, white", skinColor: "fair", eyeColor: "blue-gray", birthYear: "57BBY",
            filmIds: new[] { "phantom", "clones", "sith", "newhope" },
            starshipIds: new[] { "imperial_shuttle" });

        AddCharacter("anakin", "Anakin Skywalker", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "tatooine", height: 188, mass: 84, hairColor: "blond", skinColor: "fair", eyeColor: "blue", birthYear: "41.9BBY",
            filmIds: new[] { "phantom", "clones", "sith" });

        // More original trilogy characters
        AddCharacter("han", "Han Solo", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 180, mass: 80, hairColor: "brown", skinColor: "fair", eyeColor: "brown", birthYear: "29BBY",
            friendIds: new[] { "luke", "leia", "chewbacca" },
            filmIds: new[] { "newhope", "empire", "jedi", "awakens" },
            starshipIds: new[] { "millennium_falcon", "imperial_shuttle" });

        AddCharacter("greedo", "Greedo", "Human", new[] { "NEWHOPE" },
            height: 173, mass: 74, hairColor: "n/a", skinColor: "green", eyeColor: "black", birthYear: "44BBY",
            filmIds: new[] { "newhope" });

        AddCharacter("jabba", "Jabba Desilijic Tiure", "Human", new[] { "NEWHOPE", "JEDI" },
            height: 175, mass: 1358, eyeColor: "orange", birthYear: "600BBY",
            filmIds: new[] { "newhope", "jedi", "phantom" });

        AddCharacter("wedge", "Wedge Antilles", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 170, mass: 77, hairColor: "brown", skinColor: "fair", eyeColor: "hazel", birthYear: "21BBY",
            filmIds: new[] { "newhope", "empire", "jedi" },
            starshipIds: new[] { "xwing" });

        AddCharacter("jek", "Jek Tono Porkins", "Human", new[] { "NEWHOPE" },
            height: 180, mass: 110, hairColor: "brown", skinColor: "fair", eyeColor: "blue", birthYear: "unknown",
            filmIds: new[] { "newhope" },
            starshipIds: new[] { "xwing" });

        AddCharacter("yoda", "Yoda", "Human", new[] { "EMPIRE", "JEDI" },
            height: 66, mass: 17, hairColor: "white", skinColor: "green", eyeColor: "brown", birthYear: "896BBY",
            filmIds: new[] { "empire", "jedi", "phantom", "clones", "sith" });

        AddCharacter("palpatine", "Palpatine", "Human", new[] { "EMPIRE", "JEDI" },
            height: 170, mass: 75, hairColor: "grey", skinColor: "pale", eyeColor: "yellow", birthYear: "82BBY",
            filmIds: new[] { "empire", "jedi", "phantom", "clones", "sith", "skywalker" });

        // Empire Strikes Back characters
        AddCharacter("boba", "Boba Fett", "Human", new[] { "EMPIRE", "JEDI" },
            homePlanetId: "kamino", height: 183, mass: 78.2f, hairColor: "black", skinColor: "fair", eyeColor: "brown", birthYear: "31.5BBY",
            filmIds: new[] { "empire", "jedi", "clones" },
            starshipIds: new[] { "slave1" });

        AddCharacter("ig88", "IG-88", "Droid", new[] { "EMPIRE" },
            primaryFunction: "Assassin",
            height: 200, mass: 140, eyeColor: "red", birthYear: "15BBY",
            filmIds: new[] { "empire" });

        AddCharacter("bossk", "Bossk", "Human", new[] { "EMPIRE" },
            height: 190, mass: 113, hairColor: "none", eyeColor: "red", birthYear: "53BBY",
            filmIds: new[] { "empire" });

        AddCharacter("lando", "Lando Calrissian", "Human", new[] { "EMPIRE", "JEDI" },
            height: 177, mass: 79, hairColor: "black", skinColor: "dark", eyeColor: "brown", birthYear: "31BBY",
            filmIds: new[] { "empire", "jedi", "skywalker" },
            starshipIds: new[] { "millennium_falcon" });

        AddCharacter("lobot", "Lobot", "Human", new[] { "EMPIRE" },
            height: 175, mass: 79, hairColor: "none", skinColor: "light", eyeColor: "blue", birthYear: "37BBY",
            filmIds: new[] { "empire" });

        // Return of the Jedi characters
        AddCharacter("ackbar", "Admiral Ackbar", "Human", new[] { "JEDI" },
            height: 180, mass: 83, hairColor: "none", skinColor: "brown mottle", eyeColor: "orange", birthYear: "41BBY",
            filmIds: new[] { "jedi", "awakens", "lastjedi" });

        AddCharacter("mon-mothma", "Mon Mothma", "Human", new[] { "JEDI" },
            height: 150, mass: null, hairColor: "auburn", skinColor: "fair", eyeColor: "blue", birthYear: "48BBY",
            filmIds: new[] { "jedi", "clones", "sith" });

        AddCharacter("arvel", "Arvel Crynyd", "Human", new[] { "JEDI" },
            height: null, mass: null, hairColor: "brown", skinColor: "fair", eyeColor: "brown", birthYear: "unknown",
            filmIds: new[] { "jedi" },
            starshipIds: new[] { "awing" });

        AddCharacter("wicket", "Wicket Systri Warrick", "Human", new[] { "JEDI" },
            height: 88, mass: 20, hairColor: "brown", skinColor: "brown", eyeColor: "brown", birthYear: "8BBY",
            filmIds: new[] { "jedi" });

        AddCharacter("nien", "Nien Nunb", "Human", new[] { "JEDI" },
            height: 160, mass: 68, eyeColor: "black", birthYear: "unknown",
            filmIds: new[] { "jedi", "awakens" },
            starshipIds: new[] { "millennium_falcon" });

        AddCharacter("chewbacca", "Chewbacca", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "kashyyyk", height: 228, mass: 112, hairColor: "brown", skinColor: "unknown", eyeColor: "blue", birthYear: "200BBY",
            friendIds: new[] { "han", "luke", "leia", "r2d2" },
            filmIds: new[] { "newhope", "empire", "jedi", "sith", "awakens", "lastjedi", "skywalker" },
            starshipIds: new[] { "millennium_falcon" });

        // Prequel Trilogy Characters
        AddCharacter("quigon", "Qui-Gon Jinn", "Human", new[] { "NEWHOPE" },
            height: 193, mass: 89, hairColor: "brown", skinColor: "fair", eyeColor: "blue", birthYear: "92BBY",
            filmIds: new[] { "phantom" });

        AddCharacter("nute", "Nute Gunray", "Human", new[] { "NEWHOPE" },
            height: 191, mass: 90, eyeColor: "red", birthYear: "unknown",
            filmIds: new[] { "phantom", "clones", "sith" });

        AddCharacter("padme", "Padmé Amidala", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "naboo", height: 165, mass: 45, hairColor: "brown", skinColor: "light", eyeColor: "brown", birthYear: "46BBY",
            filmIds: new[] { "phantom", "clones", "sith" });

        AddCharacter("jarjar", "Jar Jar Binks", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "naboo", height: 196, mass: 66, eyeColor: "orange", birthYear: "52BBY",
            filmIds: new[] { "phantom", "clones" });

        AddCharacter("mace", "Mace Windu", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 188, mass: 84, hairColor: "none", skinColor: "dark", eyeColor: "brown", birthYear: "72BBY",
            filmIds: new[] { "phantom", "clones", "sith" });

        AddCharacter("kiadi", "Ki-Adi-Mundi", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 198, mass: 82, hairColor: "white", skinColor: "pale", eyeColor: "yellow", birthYear: "92BBY",
            filmIds: new[] { "phantom", "clones", "sith" });

        AddCharacter("kitfisto", "Kit Fisto", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 196, mass: 87, eyeColor: "black", birthYear: "unknown",
            filmIds: new[] { "phantom", "clones", "sith" });

        AddCharacter("aayla", "Aayla Secura", "Human", new[] { "EMPIRE", "JEDI" },
            height: 178, mass: 55, hairColor: "none", eyeColor: "brown", birthYear: "48BBY",
            filmIds: new[] { "clones", "sith" });

        AddCharacter("plokoon", "Plo Koon", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            height: 188, mass: 80, eyeColor: "black", birthYear: "22BBY",
            filmIds: new[] { "phantom", "clones", "sith" });

        AddCharacter("shaakti", "Shaak Ti", "Human", new[] { "EMPIRE", "JEDI" },
            height: 178, mass: 57, eyeColor: "black", birthYear: "unknown",
            filmIds: new[] { "clones", "sith" });

        AddCharacter("dooku", "Count Dooku", "Human", new[] { "EMPIRE", "JEDI" },
            height: 193, mass: 80, hairColor: "white", skinColor: "fair", eyeColor: "brown", birthYear: "102BBY",
            filmIds: new[] { "clones", "sith" });

        AddCharacter("grievous", "General Grievous", "Droid", new[] { "JEDI" },
            primaryFunction: "Military Commander",
            height: 216, mass: 159, eyeColor: "yellow", birthYear: "unknown",
            filmIds: new[] { "sith" });

        AddCharacter("jango", "Jango Fett", "Human", new[] { "EMPIRE" },
            height: 183, mass: 79, hairColor: "black", skinColor: "tan", eyeColor: "brown", birthYear: "66BBY",
            filmIds: new[] { "clones" },
            starshipIds: new[] { "slave1" });

        AddCharacter("zam", "Zam Wesell", "Human", new[] { "EMPIRE" },
            height: 168, mass: 55, hairColor: "blonde", eyeColor: "yellow", birthYear: "unknown",
            filmIds: new[] { "clones" });

        AddCharacter("bail", "Bail Organa", "Human", new[] { "NEWHOPE", "EMPIRE", "JEDI" },
            homePlanetId: "alderaan", height: 191, mass: null, hairColor: "black", skinColor: "tan", eyeColor: "brown", birthYear: "67BBY",
            filmIds: new[] { "clones", "sith" });

        AddCharacter("rex", "Captain Rex", "Human", new[] { "EMPIRE", "JEDI" },
            height: 183, mass: 78, hairColor: "black", skinColor: "tan", eyeColor: "brown", birthYear: "32BBY",
            filmIds: new[] { "clones", "sith" });

        AddCharacter("cody", "Commander Cody", "Human", new[] { "JEDI" },
            height: 183, mass: 78, hairColor: "black", skinColor: "tan", eyeColor: "brown", birthYear: "32BBY",
            filmIds: new[] { "sith" });

        AddCharacter("watto", "Watto", "Human", new[] { "NEWHOPE" },
            homePlanetId: "tatooine", height: 137, mass: null, eyeColor: "yellow", birthYear: "unknown",
            filmIds: new[] { "phantom", "clones" });

        AddCharacter("sebulba", "Sebulba", "Human", new[] { "NEWHOPE" },
            homePlanetId: "tatooine", height: 112, mass: 40, eyeColor: "orange", birthYear: "unknown",
            filmIds: new[] { "phantom" });

        AddCharacter("shmi", "Shmi Skywalker", "Human", new[] { "NEWHOPE", "EMPIRE" },
            homePlanetId: "tatooine", height: 163, mass: null, hairColor: "black", skinColor: "fair", eyeColor: "brown", birthYear: "72BBY",
            filmIds: new[] { "phantom", "clones" });

        // Sequel Trilogy Characters
        AddCharacter("rey", "Rey", "Human", new[] { "JEDI" },
            homePlanetId: "jakku", height: 170, mass: null, hairColor: "brown", skinColor: "light", eyeColor: "hazel", birthYear: "15ABY",
            filmIds: new[] { "awakens", "lastjedi", "skywalker" });

        AddCharacter("finn", "Finn", "Human", new[] { "JEDI" },
            height: 178, mass: 73, hairColor: "black", skinColor: "dark", eyeColor: "dark", birthYear: "11ABY",
            filmIds: new[] { "awakens", "lastjedi", "skywalker" });

        AddCharacter("poe", "Poe Dameron", "Human", new[] { "JEDI" },
            height: 172, mass: 80, hairColor: "brown", skinColor: "light", eyeColor: "brown", birthYear: "2ABY",
            filmIds: new[] { "awakens", "lastjedi", "skywalker" },
            starshipIds: new[] { "xwing" });

        AddCharacter("kylo", "Kylo Ren", "Human", new[] { "JEDI" },
            height: 189, mass: 89, hairColor: "black", skinColor: "light", eyeColor: "brown", birthYear: "5ABY",
            filmIds: new[] { "awakens", "lastjedi", "skywalker" });

        AddCharacter("bb8", "BB-8", "Droid", new[] { "JEDI" },
            primaryFunction: "Astromech",
            height: 67, mass: 18, eyeColor: "black",
            filmIds: new[] { "awakens", "lastjedi", "skywalker" });

        AddCharacter("phasma", "Captain Phasma", "Human", new[] { "JEDI" },
            height: 200, mass: null, eyeColor: "unknown", birthYear: "unknown",
            filmIds: new[] { "awakens", "lastjedi" });

        AddCharacter("snoke", "Supreme Leader Snoke", "Human", new[] { "JEDI" },
            height: 216, mass: null, hairColor: "none", eyeColor: "blue", birthYear: "unknown",
            filmIds: new[] { "awakens", "lastjedi" });

        AddCharacter("hux", "General Hux", "Human", new[] { "JEDI" },
            height: 185, mass: null, hairColor: "red", skinColor: "pale", eyeColor: "blue", birthYear: "unknown",
            filmIds: new[] { "awakens", "lastjedi", "skywalker" });

        AddCharacter("maz", "Maz Kanata", "Human", new[] { "JEDI" },
            height: 124, mass: null, eyeColor: "brown", birthYear: "unknown",
            filmIds: new[] { "awakens", "skywalker" });

        AddCharacter("rose", "Rose Tico", "Human", new[] { "JEDI" },
            height: 155, mass: null, hairColor: "black", skinColor: "light", eyeColor: "dark", birthYear: "unknown",
            filmIds: new[] { "lastjedi", "skywalker" });

        AddCharacter("holdo", "Amilyn Holdo", "Human", new[] { "JEDI" },
            height: 175, mass: null, hairColor: "purple", skinColor: "light", eyeColor: "blue", birthYear: "unknown",
            filmIds: new[] { "lastjedi" });
    }
    
    // Helper methods
    
    private void AddPlanet(string id, string name, string diameter, string rotationPeriod, 
        string orbitalPeriod, string gravity, string population, string climate, string terrain, string surfaceWater)
    {
        _planets.Add(new
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
        });
    }
    
    private void AddSpecies(string id, string name, string classification, string designation,
        string averageHeight, string averageLifespan, string eyeColors, string hairColors, 
        string skinColors, string language, string? homePlanetId)
    {
        _species.Add(new
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
        });
    }
    
    private void AddFilm(string id, string title, int episodeId, string openingCrawl,
        string director, string producer, string releaseDate)
    {
        _films.Add(new
        {
            id,
            title,
            episodeId,
            openingCrawl,
            director,
            producer,
            releaseDate
        });
    }
    
    private void AddStarship(string id, string name, string model, string starshipClass,
        string manufacturer, string costInCredits, string length, string crew, string passengers,
        string maxAtmospheringSpeed, string hyperdriveRating, string mglt, string cargoCapacity, string consumables)
    {
        _starships.Add(new
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
            mglt,
            cargoCapacity,
            consumables
        });
    }
    
    private void AddVehicle(string id, string name, string model, string vehicleClass,
        string manufacturer, string costInCredits, string length, string crew, string passengers,
        string maxAtmospheringSpeed, string cargoCapacity, string consumables)
    {
        _vehicles.Add(new
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
        });
    }
    
    private void AddCharacter(string id, string name, string characterType, string[] appearsIn,
        string? homePlanetId = null, float? height = null, float? mass = null, string? hairColor = null,
        string? skinColor = null, string? eyeColor = null, string? birthYear = null, string? primaryFunction = null,
        string[]? friendIds = null, string[]? filmIds = null, string[]? starshipIds = null, string[]? vehicleIds = null)
    {
        _characters.Add(new
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
        });
    }
}
