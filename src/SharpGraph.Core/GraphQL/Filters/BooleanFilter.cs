using System.Text.Json;

namespace SharpGraph.Core.GraphQL.Filters;

/// <summary>
/// Boolean field filter operations
/// </summary>
public class BooleanFilter
{
    public new bool? Equals { get; set; }
    public bool? Not { get; set; }

    public static BooleanFilter? FromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
        {
            // Simple boolean value means equality
            return new BooleanFilter { Equals = element.GetBoolean() };
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var filter = new BooleanFilter();

        if (element.TryGetProperty("equals", out var equals))
            filter.Equals = equals.GetBoolean();

        if (element.TryGetProperty("not", out var not))
            filter.Not = not.GetBoolean();

        return filter;
    }

    public bool Matches(bool? value)
    {
        if (value == null)
        {
            // Null checks
            return Equals == null && Not != null;
        }

        var val = value.Value;

        // Equals
        if (Equals.HasValue && val != Equals.Value)
            return false;

        // Not
        if (Not.HasValue && val == Not.Value)
            return false;

        return true;
    }

    public bool Matches(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return Matches((bool?)null);
        }

        if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
        {
            return Matches(element.GetBoolean());
        }

        return false;
    }
}
