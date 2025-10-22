using System.Text.Json;

namespace EVCharging;

public class DataGenerator
{
    private readonly Random _random;

    public DataGenerator(int seed = 42)
    {
        _random = new Random(seed); // Fixed seed for reproducibility
    }

    public string GenerateAllData()
    {
        Console.WriteLine("🔧 Generating 100,000+ records...");
        
        var persons = GeneratePersons(10000);
        var chargeCards = GenerateChargeCards(10000);
        var chargeTokens = GenerateChargeTokens(50000, 10000);
        var chargeStations = GenerateChargeStations(500);
        var connectors = GenerateConnectors(500);
        var chargeSessions = GenerateChargeSessions(100000, 50000, 2000);
        var chargeDetailRecords = GenerateChargeDetailRecords(100000, 50000, 2000);

        var data = new
        {
            Person = persons,
            ChargeCard = chargeCards,
            ChargeToken = chargeTokens,
            ChargeStation = chargeStations,
            Connector = connectors,
            ChargeSession = chargeSessions,
            ChargeDetailRecord = chargeDetailRecords
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    public string GenerateServerData()
    {
        Console.WriteLine("🔧 Generating 10,000 records (server-friendly size)...");
        
        var persons = GeneratePersons(1000);
        var chargeCards = GenerateChargeCards(1000);
        var chargeTokens = GenerateChargeTokens(5000, 1000);
        var chargeStations = GenerateChargeStations(100);
        var connectors = GenerateConnectors(100);
        var chargeSessions = GenerateChargeSessions(10000, 5000, 400);
        var chargeDetailRecords = GenerateChargeDetailRecords(10000, 5000, 400);

        var data = new
        {
            Person = persons,
            ChargeCard = chargeCards,
            ChargeToken = chargeTokens,
            ChargeStation = chargeStations,
            Connector = connectors,
            ChargeSession = chargeSessions,
            ChargeDetailRecord = chargeDetailRecords
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    private List<object> GeneratePersons(int count)
    {
        Console.WriteLine($"  👥 Generating {count:N0} persons...");
        var persons = new List<object>();
        var firstNames = new[] { "John", "Emma", "Michael", "Sophia", "William", "Olivia", "James", "Ava", "Robert", "Isabella", "Lucas", "Mia", "Alexander", "Charlotte", "Henry" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Wilson", "Anderson", "Taylor", "Thomas", "Moore" };
        var cities = new[] { "Amsterdam", "Rotterdam", "Utrecht", "The Hague", "Eindhoven", "Brussels", "Antwerp", "Paris", "Berlin", "London" };
        var countries = new[] { "Netherlands", "Belgium", "France", "Germany", "UK" };
        var statuses = new[] { "active", "active", "active", "active", "suspended" };

        for (int i = 1; i <= count; i++)
        {
            var firstName = firstNames[_random.Next(firstNames.Length)];
            var lastName = lastNames[_random.Next(lastNames.Length)];
            var city = cities[_random.Next(cities.Length)];
            
            persons.Add(new
            {
                id = $"person-{i}",
                name = $"{firstName} {lastName}",
                email = $"driver{i}@example.com",
                phone = $"+31{_random.Next(600000000, 699999999)}",
                address = $"{_random.Next(1, 999)} Main Street",
                city = city,
                country = countries[_random.Next(countries.Length)],
                registrationDate = GenerateRandomDate(2020, 2025),
                status = statuses[_random.Next(statuses.Length)]
            });
        }
        return persons;
    }

    private List<object> GenerateChargeCards(int count)
    {
        Console.WriteLine($"  💳 Generating {count:N0} charge cards...");
        var cards = new List<object>();
        var cardTypes = new[] { "RFID", "Mobile", "Credit", "Debit" };
        var issuers = new[] { "ChargePoint", "Shell Recharge", "Fastned", "IONITY", "Tesla" };
        var statuses = new[] { "active", "active", "active", "blocked", "expired" };

        for (int i = 1; i <= count; i++)
        {
            cards.Add(new
            {
                id = $"card-{i}",
                personId = $"person-{i}",
                cardNumber = $"NL-{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}",
                cardType = cardTypes[_random.Next(cardTypes.Length)],
                issuer = issuers[_random.Next(issuers.Length)],
                expiryDate = GenerateRandomDate(2025, 2030),
                status = statuses[_random.Next(statuses.Length)],
                issuedDate = GenerateRandomDate(2020, 2024)
            });
        }
        return cards;
    }

    private List<object> GenerateChargeTokens(int count, int cardCount)
    {
        Console.WriteLine($"  🎫 Generating {count:N0} charge tokens...");
        var tokens = new List<object>();
        var tokenTypes = new[] { "RFID", "NFC", "QR", "App", "Credit" };
        var statuses = new[] { "active", "active", "active", "active", "blocked" };

        for (int i = 1; i <= count; i++)
        {
            var cardId = _random.Next(1, cardCount + 1);
            tokens.Add(new
            {
                id = $"token-{i}",
                chargeCardId = $"card-{cardId}",
                tokenIdentifier = $"TOK{_random.Next(100000000, 999999999)}",
                tokenType = tokenTypes[_random.Next(tokenTypes.Length)],
                visualNumber = $"{_random.Next(1000, 9999)} **** {_random.Next(1000, 9999)}",
                status = statuses[_random.Next(statuses.Length)],
                activatedDate = GenerateRandomDate(2020, 2025),
                lastUsed = GenerateRandomDate(2024, 2025)
            });
        }
        return tokens;
    }

    private List<object> GenerateChargeStations(int count)
    {
        Console.WriteLine($"  🔌 Generating {count:N0} charge stations...");
        var stations = new List<object>();
        var operators = new[] { "Fastned", "IONITY", "Shell Recharge", "Allego", "ChargePoint", "Tesla Supercharger" };
        var cities = new[] { "Amsterdam", "Rotterdam", "Utrecht", "The Hague", "Eindhoven", "Brussels", "Antwerp", "Paris", "Berlin", "London" };
        var statuses = new[] { "operational", "operational", "operational", "maintenance", "offline" };
        
        for (int i = 1; i <= count; i++)
        {
            var city = cities[_random.Next(cities.Length)];
            var totalConn = _random.Next(4, 13);
            
            stations.Add(new
            {
                id = $"station-{i}",
                name = $"{city} Station {i}",
                @operator = operators[_random.Next(operators.Length)],
                location = $"{city} {(_random.Next(2) == 0 ? "North" : "South")}",
                address = $"{_random.Next(1, 500)} Highway A{_random.Next(1, 20)}",
                city = city,
                country = DetermineCityCountry(city),
                latitude = 48.0 + _random.NextDouble() * 5.0,
                longitude = 2.0 + _random.NextDouble() * 8.0,
                status = statuses[_random.Next(statuses.Length)],
                totalConnectors = totalConn,
                availableConnectors = _random.Next(0, totalConn + 1),
                openingHours = "24/7"
            });
        }
        return stations;
    }

    private List<object> GenerateConnectors(int stationCount)
    {
        Console.WriteLine($"  🔌 Generating ~2000 connectors...");
        var connectors = new List<object>();
        var connectorTypes = new[] { "Type2", "CCS", "CHAdeMO", "Tesla" };
        var statuses = new[] { "available", "available", "occupied", "faulted", "reserved" };
        
        int connectorId = 1;
        for (int stationId = 1; stationId <= stationCount; stationId++)
        {
            int connectorCount = _random.Next(2, 6);
            
            for (int connNum = 1; connNum <= connectorCount; connNum++)
            {
                var connType = connectorTypes[_random.Next(connectorTypes.Length)];
                var powerType = connType == "Type2" ? "AC" : "DC";
                var maxPower = powerType == "AC" ? 22.0 : (_random.Next(2) == 0 ? 50.0 : 150.0);
                
                connectors.Add(new
                {
                    id = $"connector-{connectorId}",
                    chargeStationId = $"station-{stationId}",
                    connectorNumber = connNum,
                    connectorType = connType,
                    powerType = powerType,
                    maxPower = maxPower,
                    voltage = powerType == "AC" ? 230 : 400,
                    amperage = powerType == "AC" ? 32 : 125,
                    status = statuses[_random.Next(statuses.Length)],
                    pricePerKwh = Math.Round(0.25 + _random.NextDouble() * 0.30, 3),
                    pricePerMinute = Math.Round(_random.NextDouble() * 0.10, 3)
                });
                connectorId++;
            }
        }
        return connectors;
    }

    private List<object> GenerateChargeSessions(int count, int tokenCount, int connectorCount)
    {
        Console.WriteLine($"  ⚡ Generating {count:N0} charge sessions...");
        var sessions = new List<object>();
        var statuses = new[] { "completed", "completed", "completed", "charging", "failed" };
        var authMethods = new[] { "RFID", "App", "QR", "Credit" };

        for (int i = 1; i <= count; i++)
        {
            var status = statuses[_random.Next(statuses.Length)];
            var startTime = GenerateRandomDateTime(2024, 2025);
            var duration = _random.Next(15, 480);
            
            var connectorId = _random.Next(1, connectorCount + 1);
            var stationId = (connectorId - 1) / 4 + 1;
            
            sessions.Add(new
            {
                id = $"session-{i}",
                chargeTokenId = $"token-{_random.Next(1, tokenCount + 1)}",
                connectorId = $"connector-{connectorId}",
                chargeStationId = $"station-{stationId}",
                status = status,
                startTime = startTime,
                endTime = status == "completed" ? AddMinutes(startTime, duration) : null,
                authMethod = authMethods[_random.Next(authMethods.Length)],
                meterStartValue = Math.Round(_random.NextDouble() * 100000.0, 2),
                meterEndValue = status == "completed" ? Math.Round(_random.NextDouble() * 100000.0 + 50.0, 2) : (double?)null,
                reservationId = _random.Next(100) < 20 ? $"RES{_random.Next(100000, 999999)}" : null
            });
        }
        return sessions;
    }

    private List<object> GenerateChargeDetailRecords(int count, int tokenCount, int connectorCount)
    {
        Console.WriteLine($"  📊 Generating {count:N0} charge detail records (CDRs)...");
        var cdrs = new List<object>();
        var statuses = new[] { "completed", "completed", "completed", "failed", "disputed" };
        var authMethods = new[] { "RFID", "App", "QR", "Credit" };
        var currencies = new[] { "EUR", "EUR", "EUR", "GBP", "USD" };

        for (int i = 1; i <= count; i++)
        {
            var startTime = GenerateRandomDateTime(2024, 2025);
            var duration = _random.Next(15, 480);
            var endTime = AddMinutes(startTime, duration);
            
            var energyDelivered = _random.Next(5, 85) + _random.NextDouble();
            var pricePerKwh = 0.25 + _random.NextDouble() * 0.30;
            var totalCost = energyDelivered * pricePerKwh;
            
            var connectorId = _random.Next(1, connectorCount + 1);
            var stationId = (connectorId - 1) / 4 + 1;
            
            cdrs.Add(new
            {
                id = $"cdr-{i}",
                chargeSessionId = $"session-{i}",
                chargeTokenId = $"token-{_random.Next(1, tokenCount + 1)}",
                connectorId = $"connector-{connectorId}",
                chargeStationId = $"station-{stationId}",
                startTime = startTime,
                endTime = endTime,
                duration = duration,
                energyDelivered = Math.Round(energyDelivered, 2),
                meterStartValue = Math.Round(_random.NextDouble() * 100000.0, 2),
                meterEndValue = Math.Round(_random.NextDouble() * 100000.0 + energyDelivered, 2),
                pricePerKwh = Math.Round(pricePerKwh, 3),
                totalCost = Math.Round(totalCost, 2),
                currency = currencies[_random.Next(currencies.Length)],
                status = statuses[_random.Next(statuses.Length)],
                authMethod = authMethods[_random.Next(authMethods.Length)],
                tariffDescription = $"Standard Rate - {Math.Round(pricePerKwh, 2)} per kWh"
            });
        }
        return cdrs;
    }

    private string GenerateRandomDate(int startYear, int endYear)
    {
        var start = new DateTime(startYear, 1, 1);
        var end = new DateTime(endYear, 12, 31);
        var range = (end - start).Days;
        return start.AddDays(_random.Next(range)).ToString("yyyy-MM-dd");
    }

    private string GenerateRandomDateTime(int startYear, int endYear)
    {
        var start = new DateTime(startYear, 1, 1);
        var end = new DateTime(endYear, 12, 31);
        var range = (end - start).Days;
        var date = start.AddDays(_random.Next(range));
        var hour = _random.Next(0, 24);
        var minute = _random.Next(0, 60);
        var second = _random.Next(0, 60);
        return new DateTime(date.Year, date.Month, date.Day, hour, minute, second).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private string AddMinutes(string dateTime, int minutes)
    {
        var dt = DateTime.Parse(dateTime);
        return dt.AddMinutes(minutes).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private string DetermineCityCountry(string city)
    {
        return city switch
        {
            "Amsterdam" or "Rotterdam" or "Utrecht" or "The Hague" or "Eindhoven" => "Netherlands",
            "Brussels" or "Antwerp" => "Belgium",
            "Paris" => "France",
            "Berlin" => "Germany",
            "London" => "UK",
            _ => "Netherlands"
        };
    }
}
