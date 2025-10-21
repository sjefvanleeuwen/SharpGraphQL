using System.Text.Json;
using Xunit;
using SharpGraph.Core.GraphQL.Filters;

namespace SharpGraph.Tests;

public class FilteringTests
{
    [Fact]
    public void StringFilter_Equals_Works()
    {
        var filter = new StringFilter { Equals = "Luke Skywalker" };
        
        Assert.True(filter.Matches("Luke Skywalker"));
        Assert.False(filter.Matches("Darth Vader"));
        Assert.False(filter.Matches(null));
    }

    [Fact]
    public void StringFilter_Contains_Works()
    {
        var filter = new StringFilter { Contains = "Skywalker" };
        
        Assert.True(filter.Matches("Luke Skywalker"));
        Assert.True(filter.Matches("Anakin Skywalker"));
        Assert.False(filter.Matches("Darth Vader"));
    }

    [Fact]
    public void StringFilter_StartsWith_Works()
    {
        var filter = new StringFilter { StartsWith = "Luke" };
        
        Assert.True(filter.Matches("Luke Skywalker"));
        Assert.False(filter.Matches("Darth Luke"));
        Assert.False(filter.Matches("luke skywalker")); // Case sensitive by default
    }

    [Fact]
    public void StringFilter_EndsWith_Works()
    {
        var filter = new StringFilter { EndsWith = "Skywalker" };
        
        Assert.True(filter.Matches("Luke Skywalker"));
        Assert.True(filter.Matches("Anakin Skywalker"));
        Assert.False(filter.Matches("Skywalker Luke"));
    }

    [Fact]
    public void StringFilter_CaseInsensitive_Works()
    {
        var filter = new StringFilter 
        { 
            Contains = "skywalker",
            Mode = QueryMode.Insensitive
        };
        
        Assert.True(filter.Matches("Luke SKYWALKER"));
        Assert.True(filter.Matches("ANAKIN Skywalker"));
    }

    [Fact]
    public void StringFilter_In_Works()
    {
        var filter = new StringFilter { In = new List<string> { "Luke", "Leia", "Han" } };
        
        Assert.True(filter.Matches("Luke"));
        Assert.True(filter.Matches("Leia"));
        Assert.False(filter.Matches("Darth"));
    }

    [Fact]
    public void IntFilter_Equals_Works()
    {
        var filter = new IntFilter { Equals = 4 };
        
        Assert.True(filter.Matches(4));
        Assert.False(filter.Matches(5));
    }

    [Fact]
    public void IntFilter_GreaterThan_Works()
    {
        var filter = new IntFilter { Gt = 170 };
        
        Assert.True(filter.Matches(180));
        Assert.True(filter.Matches(200));
        Assert.False(filter.Matches(170));
        Assert.False(filter.Matches(160));
    }

    [Fact]
    public void IntFilter_GreaterThanOrEqual_Works()
    {
        var filter = new IntFilter { Gte = 170 };
        
        Assert.True(filter.Matches(170));
        Assert.True(filter.Matches(180));
        Assert.False(filter.Matches(160));
    }

    [Fact]
    public void IntFilter_LessThan_Works()
    {
        var filter = new IntFilter { Lt = 100 };
        
        Assert.True(filter.Matches(50));
        Assert.True(filter.Matches(99));
        Assert.False(filter.Matches(100));
        Assert.False(filter.Matches(150));
    }

    [Fact]
    public void IntFilter_In_Works()
    {
        var filter = new IntFilter { In = new List<int> { 4, 5, 6 } };
        
        Assert.True(filter.Matches(4));
        Assert.True(filter.Matches(5));
        Assert.True(filter.Matches(6));
        Assert.False(filter.Matches(1));
        Assert.False(filter.Matches(7));
    }

    [Fact]
    public void FloatFilter_GreaterThan_Works()
    {
        var filter = new FloatFilter { Gt = 170.5 };
        
        Assert.True(filter.Matches(180.0));
        Assert.True(filter.Matches(171.0));
        Assert.False(filter.Matches(170.5));
        Assert.False(filter.Matches(160.0));
    }

    [Fact]
    public void BooleanFilter_Equals_Works()
    {
        var trueFilter = new BooleanFilter { Equals = true };
        var falseFilter = new BooleanFilter { Equals = false };
        
        Assert.True(trueFilter.Matches(true));
        Assert.False(trueFilter.Matches(false));
        
        Assert.True(falseFilter.Matches(false));
        Assert.False(falseFilter.Matches(true));
    }

    [Fact]
    public void FilterEvaluator_SimpleStringFilter_Works()
    {
        var record = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Luke Skywalker"),
            ["characterType"] = JsonSerializer.SerializeToElement("Human")
        };

        var whereJson = JsonSerializer.Serialize(new
        {
            name = new { contains = "Skywalker" }
        });

        var whereClause = JsonSerializer.Deserialize<JsonElement>(whereJson);
        
        Assert.True(FilterEvaluator.Matches(record, whereClause));
    }

    [Fact]
    public void FilterEvaluator_MultipleConditions_Works()
    {
        var record = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Luke Skywalker"),
            ["characterType"] = JsonSerializer.SerializeToElement("Human"),
            ["height"] = JsonSerializer.SerializeToElement(172)
        };

        var whereJson = JsonSerializer.Serialize(new
        {
            name = new { contains = "Skywalker" },
            characterType = "Human",
            height = new { gte = 170 }
        });

        var whereClause = JsonSerializer.Deserialize<JsonElement>(whereJson);
        
        Assert.True(FilterEvaluator.Matches(record, whereClause));
    }

    [Fact]
    public void FilterEvaluator_OrCondition_Works()
    {
        var droid = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("C-3PO"),
            ["characterType"] = JsonSerializer.SerializeToElement("Droid")
        };

        var human = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Luke"),
            ["characterType"] = JsonSerializer.SerializeToElement("Human")
        };

        var whereJson = JsonSerializer.Serialize(new
        {
            OR = new object[]
            {
                new { characterType = "Droid" },
                new { characterType = "Human" }
            }
        });

        var whereClause = JsonSerializer.Deserialize<JsonElement>(whereJson);
        
        Assert.True(FilterEvaluator.Matches(droid, whereClause));
        Assert.True(FilterEvaluator.Matches(human, whereClause));
    }

    [Fact]
    public void FilterEvaluator_AndCondition_Works()
    {
        var match = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Luke Skywalker"),
            ["characterType"] = JsonSerializer.SerializeToElement("Human"),
            ["height"] = JsonSerializer.SerializeToElement(172)
        };

        var noMatch = new Dictionary<string, JsonElement>
        {
            ["name"] = JsonSerializer.SerializeToElement("Luke Skywalker"),
            ["characterType"] = JsonSerializer.SerializeToElement("Human"),
            ["height"] = JsonSerializer.SerializeToElement(160)
        };

        var whereJson = JsonSerializer.Serialize(new
        {
            AND = new object[]
            {
                new { characterType = "Human" },
                new { height = new { gte = 170 } }
            }
        });

        var whereClause = JsonSerializer.Deserialize<JsonElement>(whereJson);
        
        Assert.True(FilterEvaluator.Matches(match, whereClause));
        Assert.False(FilterEvaluator.Matches(noMatch, whereClause));
    }

    [Fact]
    public void FilterEvaluator_NotCondition_Works()
    {
        var droid = new Dictionary<string, JsonElement>
        {
            ["characterType"] = JsonSerializer.SerializeToElement("Droid")
        };

        var human = new Dictionary<string, JsonElement>
        {
            ["characterType"] = JsonSerializer.SerializeToElement("Human")
        };

        var whereJson = JsonSerializer.Serialize(new
        {
            NOT = new { characterType = "Droid" }
        });

        var whereClause = JsonSerializer.Deserialize<JsonElement>(whereJson);
        
        Assert.False(FilterEvaluator.Matches(droid, whereClause));
        Assert.True(FilterEvaluator.Matches(human, whereClause));
    }

    [Fact]
    public void FilterEvaluator_ApplyFilters_Works()
    {
        var records = new List<Dictionary<string, JsonElement>>
        {
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Luke Skywalker"),
                ["height"] = JsonSerializer.SerializeToElement(172)
            },
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Darth Vader"),
                ["height"] = JsonSerializer.SerializeToElement(202)
            },
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Leia Organa"),
                ["height"] = JsonSerializer.SerializeToElement(150)
            }
        };

        var whereJson = JsonSerializer.Serialize(new
        {
            height = new { gte = 170 }
        });

        var whereClause = JsonSerializer.Deserialize<JsonElement>(whereJson);
        var filtered = FilterEvaluator.ApplyFilters(records, whereClause);
        
        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, r => r["name"].GetString() == "Luke Skywalker");
        Assert.Contains(filtered, r => r["name"].GetString() == "Darth Vader");
    }

    [Fact]
    public void FilterEvaluator_ApplySorting_Ascending_Works()
    {
        var records = new List<Dictionary<string, JsonElement>>
        {
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Darth Vader"),
                ["height"] = JsonSerializer.SerializeToElement(202)
            },
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Luke Skywalker"),
                ["height"] = JsonSerializer.SerializeToElement(172)
            },
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Leia Organa"),
                ["height"] = JsonSerializer.SerializeToElement(150)
            }
        };

        var orderByJson = JsonSerializer.Serialize(new { name = "asc" });
        var orderBy = JsonSerializer.Deserialize<JsonElement>(orderByJson);
        var sorted = FilterEvaluator.ApplySorting(records, orderBy);
        
        Assert.Equal("Darth Vader", sorted[0]["name"].GetString());
        Assert.Equal("Leia Organa", sorted[1]["name"].GetString());
        Assert.Equal("Luke Skywalker", sorted[2]["name"].GetString());
    }

    [Fact]
    public void FilterEvaluator_ApplySorting_Descending_Works()
    {
        var records = new List<Dictionary<string, JsonElement>>
        {
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Luke"),
                ["height"] = JsonSerializer.SerializeToElement(172)
            },
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Vader"),
                ["height"] = JsonSerializer.SerializeToElement(202)
            },
            new()
            {
                ["name"] = JsonSerializer.SerializeToElement("Leia"),
                ["height"] = JsonSerializer.SerializeToElement(150)
            }
        };

        var orderByJson = JsonSerializer.Serialize(new { height = "desc" });
        var orderBy = JsonSerializer.Deserialize<JsonElement>(orderByJson);
        var sorted = FilterEvaluator.ApplySorting(records, orderBy);
        
        Assert.Equal(202, sorted[0]["height"].GetInt32());
        Assert.Equal(172, sorted[1]["height"].GetInt32());
        Assert.Equal(150, sorted[2]["height"].GetInt32());
    }

    [Fact]
    public void FilterEvaluator_ApplyPagination_Works()
    {
        var records = new List<Dictionary<string, JsonElement>>
        {
            new() { ["id"] = JsonSerializer.SerializeToElement("1") },
            new() { ["id"] = JsonSerializer.SerializeToElement("2") },
            new() { ["id"] = JsonSerializer.SerializeToElement("3") },
            new() { ["id"] = JsonSerializer.SerializeToElement("4") },
            new() { ["id"] = JsonSerializer.SerializeToElement("5") }
        };

        var paginated = FilterEvaluator.ApplyPagination(records, skip: 2, take: 2);
        
        Assert.Equal(2, paginated.Count);
        Assert.Equal("3", paginated[0]["id"].GetString());
        Assert.Equal("4", paginated[1]["id"].GetString());
    }
}
