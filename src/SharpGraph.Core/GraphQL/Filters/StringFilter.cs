using System.Text.Json;

namespace SharpGraph.Core.GraphQL.Filters;

/// <summary>
/// String field filter operations
/// </summary>
public class StringFilter
{
    public new string? Equals { get; set; }
    public string? Not { get; set; }
    public List<string>? In { get; set; }
    public List<string>? NotIn { get; set; }
    public string? Lt { get; set; }
    public string? Lte { get; set; }
    public string? Gt { get; set; }
    public string? Gte { get; set; }
    public string? Contains { get; set; }
    public string? StartsWith { get; set; }
    public string? EndsWith { get; set; }
    public QueryMode Mode { get; set; } = QueryMode.Default;

    public static StringFilter? FromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            // Simple string value means equality
            return new StringFilter { Equals = element.GetString() };
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var filter = new StringFilter();

        if (element.TryGetProperty("equals", out var equals))
            filter.Equals = equals.GetString();

        if (element.TryGetProperty("not", out var not))
            filter.Not = not.GetString();

        if (element.TryGetProperty("in", out var inProp) && inProp.ValueKind == JsonValueKind.Array)
            filter.In = inProp.EnumerateArray().Select(e => e.GetString()!).ToList();

        if (element.TryGetProperty("notIn", out var notIn) && notIn.ValueKind == JsonValueKind.Array)
            filter.NotIn = notIn.EnumerateArray().Select(e => e.GetString()!).ToList();

        if (element.TryGetProperty("lt", out var lt))
            filter.Lt = lt.GetString();

        if (element.TryGetProperty("lte", out var lte))
            filter.Lte = lte.GetString();

        if (element.TryGetProperty("gt", out var gt))
            filter.Gt = gt.GetString();

        if (element.TryGetProperty("gte", out var gte))
            filter.Gte = gte.GetString();

        if (element.TryGetProperty("contains", out var contains))
            filter.Contains = contains.GetString();

        if (element.TryGetProperty("startsWith", out var startsWith))
            filter.StartsWith = startsWith.GetString();

        if (element.TryGetProperty("endsWith", out var endsWith))
            filter.EndsWith = endsWith.GetString();

        if (element.TryGetProperty("mode", out var mode))
        {
            var modeStr = mode.GetString();
            filter.Mode = modeStr?.ToLower() == "insensitive" ? QueryMode.Insensitive : QueryMode.Default;
        }

        return filter;
    }

    public bool Matches(string? value)
    {
        if (value == null)
        {
            // Null checks
            return Equals == null && Not != null;
        }

        var comparison = Mode == QueryMode.Insensitive 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;

        // Equals
        if (Equals != null && !string.Equals(value, Equals, comparison))
            return false;

        // Not
        if (Not != null && string.Equals(value, Not, comparison))
            return false;

        // In
        if (In != null && !In.Any(v => string.Equals(value, v, comparison)))
            return false;

        // NotIn
        if (NotIn != null && NotIn.Any(v => string.Equals(value, v, comparison)))
            return false;

        // String comparisons (ordinal)
        if (Lt != null && string.Compare(value, Lt, comparison) >= 0)
            return false;

        if (Lte != null && string.Compare(value, Lte, comparison) > 0)
            return false;

        if (Gt != null && string.Compare(value, Gt, comparison) <= 0)
            return false;

        if (Gte != null && string.Compare(value, Gte, comparison) < 0)
            return false;

        // Contains
        if (Contains != null && !value.Contains(Contains, comparison))
            return false;

        // StartsWith
        if (StartsWith != null && !value.StartsWith(StartsWith, comparison))
            return false;

        // EndsWith
        if (EndsWith != null && !value.EndsWith(EndsWith, comparison))
            return false;

        return true;
    }
}

public enum QueryMode
{
    Default,
    Insensitive
}

