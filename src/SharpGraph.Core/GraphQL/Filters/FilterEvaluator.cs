using System.Text.Json;

namespace SharpGraph.Core.GraphQL.Filters;

/// <summary>
/// Evaluates filter expressions against data records
/// </summary>
public class FilterEvaluator
{
    /// <summary>
    /// Evaluates a where clause against a record
    /// </summary>
    public static bool Matches(Dictionary<string, JsonElement> record, JsonElement whereClause)
    {
        if (whereClause.ValueKind != JsonValueKind.Object)
        {
            return true; // No filter means match all
        }

        // Process each filter condition
        foreach (var property in whereClause.EnumerateObject())
        {
            var fieldName = property.Name;
            var filterValue = property.Value;

            // Handle logical operators
            if (fieldName == "AND")
            {
                if (!EvaluateAnd(record, filterValue))
                    return false;
                continue;
            }

            if (fieldName == "OR")
            {
                if (!EvaluateOr(record, filterValue))
                    return false;
                continue;
            }

            if (fieldName == "NOT")
            {
                if (Matches(record, filterValue))
                    return false;
                continue;
            }

            // Get the field value from the record
            if (!record.TryGetValue(fieldName, out var fieldValue))
            {
                // Field doesn't exist - treat as null
                fieldValue = default;
            }

            // Evaluate the filter based on field type
            if (!EvaluateFieldFilter(fieldValue, filterValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateAnd(Dictionary<string, JsonElement> record, JsonElement andClause)
    {
        if (andClause.ValueKind != JsonValueKind.Array)
            return true;

        // All conditions must match
        foreach (var condition in andClause.EnumerateArray())
        {
            if (!Matches(record, condition))
                return false;
        }

        return true;
    }

    private static bool EvaluateOr(Dictionary<string, JsonElement> record, JsonElement orClause)
    {
        if (orClause.ValueKind != JsonValueKind.Array)
            return true;

        // At least one condition must match
        foreach (var condition in orClause.EnumerateArray())
        {
            if (Matches(record, condition))
                return true;
        }

        return false; // None matched
    }

    private static bool EvaluateFieldFilter(JsonElement fieldValue, JsonElement filterValue)
    {
        // Determine field type and apply appropriate filter
        var fieldKind = fieldValue.ValueKind;

        // Handle null/undefined
        if (fieldKind == JsonValueKind.Null || fieldKind == JsonValueKind.Undefined)
        {
            return EvaluateNullFilter(filterValue);
        }

        // String fields
        if (fieldKind == JsonValueKind.String)
        {
            var filter = StringFilter.FromJson(filterValue);
            return filter?.Matches(fieldValue.GetString()) ?? true;
        }

        // Numeric fields
        if (fieldKind == JsonValueKind.Number)
        {
            // Try int first, then float
            if (fieldValue.TryGetInt32(out var intVal))
            {
                var intFilter = IntFilter.FromJson(filterValue);
                return intFilter?.Matches(intVal) ?? true;
            }
            else
            {
                var floatFilter = FloatFilter.FromJson(filterValue);
                return floatFilter?.Matches(fieldValue.GetDouble()) ?? true;
            }
        }

        // Boolean fields
        if (fieldKind == JsonValueKind.True || fieldKind == JsonValueKind.False)
        {
            var filter = BooleanFilter.FromJson(filterValue);
            return filter?.Matches(fieldValue.GetBoolean()) ?? true;
        }

        // For other types (arrays, objects), just check equality for now
        if (filterValue.ValueKind == JsonValueKind.Object)
        {
            // Could be a complex filter - for Phase 1, skip
            return true;
        }

        // Simple equality check
        return JsonSerializer.Serialize(fieldValue) == JsonSerializer.Serialize(filterValue);
    }

    private static bool EvaluateNullFilter(JsonElement filterValue)
    {
        // Check if filter is checking for null
        if (filterValue.ValueKind == JsonValueKind.Null)
        {
            return true; // Field is null and filter expects null
        }

        if (filterValue.ValueKind == JsonValueKind.Object)
        {
            // Check for { not: value } - field is null, so if "not" is specified, depends on value
            if (filterValue.TryGetProperty("not", out var notValue))
            {
                return notValue.ValueKind != JsonValueKind.Null;
            }
        }

        return false; // Field is null but filter expects a value
    }

    /// <summary>
    /// Applies filters to a collection of records
    /// </summary>
    public static List<Dictionary<string, JsonElement>> ApplyFilters(
        List<Dictionary<string, JsonElement>> records,
        JsonElement? whereClause)
    {
        if (whereClause == null || whereClause.Value.ValueKind == JsonValueKind.Null)
        {
            return records; // No filtering
        }

        return records.Where(record => Matches(record, whereClause.Value)).ToList();
    }

    /// <summary>
    /// Applies sorting to a collection of records
    /// </summary>
    public static List<Dictionary<string, JsonElement>> ApplySorting(
        List<Dictionary<string, JsonElement>> records,
        JsonElement? orderBy)
    {
        if (orderBy == null || orderBy.Value.ValueKind == JsonValueKind.Null)
        {
            return records; // No sorting
        }

        var ordered = records.AsEnumerable();

        // Handle array of sort orders
        if (orderBy.Value.ValueKind == JsonValueKind.Array)
        {
            IOrderedEnumerable<Dictionary<string, JsonElement>>? orderedEnum = null;

            foreach (var sortSpec in orderBy.Value.EnumerateArray())
            {
                if (sortSpec.ValueKind != JsonValueKind.Object)
                    continue;

                foreach (var prop in sortSpec.EnumerateObject())
                {
                    var fieldName = prop.Name;
                    var direction = prop.Value.GetString()?.ToLower() ?? "asc";

                    if (orderedEnum == null)
                    {
                        // First sort
                        orderedEnum = direction == "desc"
                            ? ordered.OrderByDescending(r => GetSortValue(r, fieldName))
                            : ordered.OrderBy(r => GetSortValue(r, fieldName));
                    }
                    else
                    {
                        // Subsequent sorts
                        orderedEnum = direction == "desc"
                            ? orderedEnum.ThenByDescending(r => GetSortValue(r, fieldName))
                            : orderedEnum.ThenBy(r => GetSortValue(r, fieldName));
                    }
                }
            }

            return orderedEnum?.ToList() ?? records;
        }

        // Handle single sort order object
        if (orderBy.Value.ValueKind == JsonValueKind.Object)
        {
            IOrderedEnumerable<Dictionary<string, JsonElement>>? orderedEnum = null;

            foreach (var prop in orderBy.Value.EnumerateObject())
            {
                var fieldName = prop.Name;
                var direction = prop.Value.GetString()?.ToLower() ?? "asc";

                if (orderedEnum == null)
                {
                    orderedEnum = direction == "desc"
                        ? ordered.OrderByDescending(r => GetSortValue(r, fieldName))
                        : ordered.OrderBy(r => GetSortValue(r, fieldName));
                }
                else
                {
                    orderedEnum = direction == "desc"
                        ? orderedEnum.ThenByDescending(r => GetSortValue(r, fieldName))
                        : orderedEnum.ThenBy(r => GetSortValue(r, fieldName));
                }
            }

            return orderedEnum?.ToList() ?? records;
        }

        return records;
    }

    private static IComparable? GetSortValue(Dictionary<string, JsonElement> record, string fieldName)
    {
        if (!record.TryGetValue(fieldName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt32(out var i) ? i : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    /// <summary>
    /// Applies pagination to a collection of records
    /// </summary>
    public static List<Dictionary<string, JsonElement>> ApplyPagination(
        List<Dictionary<string, JsonElement>> records,
        int? skip,
        int? take)
    {
        var result = records.AsEnumerable();

        if (skip.HasValue && skip.Value > 0)
        {
            result = result.Skip(skip.Value);
        }

        if (take.HasValue && take.Value > 0)
        {
            result = result.Take(take.Value);
        }

        return result.ToList();
    }
}
