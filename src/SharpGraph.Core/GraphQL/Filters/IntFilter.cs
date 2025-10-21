using System.Text.Json;

namespace SharpGraph.Core.GraphQL.Filters;

/// <summary>
/// Integer field filter operations
/// </summary>
public class IntFilter
{
    public new int? Equals { get; set; }
    public int? Not { get; set; }
    public List<int>? In { get; set; }
    public List<int>? NotIn { get; set; }
    public int? Lt { get; set; }
    public int? Lte { get; set; }
    public int? Gt { get; set; }
    public int? Gte { get; set; }

    public static IntFilter? FromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            // Simple number value means equality
            return new IntFilter { Equals = element.GetInt32() };
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var filter = new IntFilter();

        if (element.TryGetProperty("equals", out var equals))
            filter.Equals = equals.GetInt32();

        if (element.TryGetProperty("not", out var not))
            filter.Not = not.GetInt32();

        if (element.TryGetProperty("in", out var inProp) && inProp.ValueKind == JsonValueKind.Array)
            filter.In = inProp.EnumerateArray().Select(e => e.GetInt32()).ToList();

        if (element.TryGetProperty("notIn", out var notIn) && notIn.ValueKind == JsonValueKind.Array)
            filter.NotIn = notIn.EnumerateArray().Select(e => e.GetInt32()).ToList();

        if (element.TryGetProperty("lt", out var lt))
            filter.Lt = lt.GetInt32();

        if (element.TryGetProperty("lte", out var lte))
            filter.Lte = lte.GetInt32();

        if (element.TryGetProperty("gt", out var gt))
            filter.Gt = gt.GetInt32();

        if (element.TryGetProperty("gte", out var gte))
            filter.Gte = gte.GetInt32();

        return filter;
    }

    public bool Matches(int? value)
    {
        if (value == null)
        {
            // Null checks - only match if we're checking for not-equals
            return Equals == null && Not != null;
        }

        var val = value.Value;

        // Equals
        if (Equals.HasValue && val != Equals.Value)
            return false;

        // Not
        if (Not.HasValue && val == Not.Value)
            return false;

        // In
        if (In != null && !In.Contains(val))
            return false;

        // NotIn
        if (NotIn != null && NotIn.Contains(val))
            return false;

        // Less than
        if (Lt.HasValue && val >= Lt.Value)
            return false;

        // Less than or equal
        if (Lte.HasValue && val > Lte.Value)
            return false;

        // Greater than
        if (Gt.HasValue && val <= Gt.Value)
            return false;

        // Greater than or equal
        if (Gte.HasValue && val < Gte.Value)
            return false;

        return true;
    }

    public bool Matches(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return Matches((int?)null);
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            return Matches(element.GetInt32());
        }

        return false;
    }
}
