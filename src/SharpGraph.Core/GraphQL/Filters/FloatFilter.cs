using System.Text.Json;

namespace SharpGraph.Core.GraphQL.Filters;

/// <summary>
/// Float/Double field filter operations
/// </summary>
public class FloatFilter
{
    public new double? Equals { get; set; }
    public double? Not { get; set; }
    public List<double>? In { get; set; }
    public List<double>? NotIn { get; set; }
    public double? Lt { get; set; }
    public double? Lte { get; set; }
    public double? Gt { get; set; }
    public double? Gte { get; set; }

    public static FloatFilter? FromJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            // Simple number value means equality
            return new FloatFilter { Equals = element.GetDouble() };
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var filter = new FloatFilter();

        if (element.TryGetProperty("equals", out var equals))
            filter.Equals = equals.GetDouble();

        if (element.TryGetProperty("not", out var not))
            filter.Not = not.GetDouble();

        if (element.TryGetProperty("in", out var inProp) && inProp.ValueKind == JsonValueKind.Array)
            filter.In = inProp.EnumerateArray().Select(e => e.GetDouble()).ToList();

        if (element.TryGetProperty("notIn", out var notIn) && notIn.ValueKind == JsonValueKind.Array)
            filter.NotIn = notIn.EnumerateArray().Select(e => e.GetDouble()).ToList();

        if (element.TryGetProperty("lt", out var lt))
            filter.Lt = lt.GetDouble();

        if (element.TryGetProperty("lte", out var lte))
            filter.Lte = lte.GetDouble();

        if (element.TryGetProperty("gt", out var gt))
            filter.Gt = gt.GetDouble();

        if (element.TryGetProperty("gte", out var gte))
            filter.Gte = gte.GetDouble();

        return filter;
    }

    public bool Matches(double? value)
    {
        if (value == null)
        {
            // Null checks
            return Equals == null && Not != null;
        }

        var val = value.Value;

        // Equals (with small epsilon for floating point comparison)
        if (Equals.HasValue && Math.Abs(val - Equals.Value) > double.Epsilon)
            return false;

        // Not
        if (Not.HasValue && Math.Abs(val - Not.Value) <= double.Epsilon)
            return false;

        // In
        if (In != null && !In.Any(v => Math.Abs(val - v) <= double.Epsilon))
            return false;

        // NotIn
        if (NotIn != null && NotIn.Any(v => Math.Abs(val - v) <= double.Epsilon))
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
            return Matches((double?)null);
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            return Matches(element.GetDouble());
        }

        return false;
    }
}

