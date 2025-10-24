using System.Text.RegularExpressions;
using SharpGraph.Db.Storage;

namespace SharpGraph.Core;

/// <summary>
/// Parses GraphQL schema definition language (SDL) files to extract type definitions,
/// fields, and relationships for automatic database table generation.
/// </summary>
public class GraphQLSchemaParser
{
    private readonly string _schemaContent;
    
    public GraphQLSchemaParser(string schemaContent)
    {
        _schemaContent = schemaContent;
    }
    
    /// <summary>
    /// Parses the GraphQL schema and returns all type definitions
    /// </summary>
    public List<ParsedType> ParseTypes()
    {
        var types = new List<ParsedType>();
        
        // Remove comments
        var content = RemoveComments(_schemaContent);
        
        // Match type definitions: type TypeName { ... }
        // But NOT input types
        var typePattern = @"(?<!input\s)type\s+(\w+)\s*\{([^}]+)\}";
        var typeMatches = Regex.Matches(content, typePattern, RegexOptions.Singleline);
        
        foreach (Match typeMatch in typeMatches)
        {
            var typeName = typeMatch.Groups[1].Value;
            var fieldsContent = typeMatch.Groups[2].Value;
            
            var parsedType = new ParsedType
            {
                Name = typeName,
                Fields = ParseFields(fieldsContent)
            };
            
            types.Add(parsedType);
        }
        
        return types;
    }
    
    /// <summary>
    /// Parses enum definitions from the schema
    /// </summary>
    public List<ParsedEnum> ParseEnums()
    {
        var enums = new List<ParsedEnum>();
        
        var content = RemoveComments(_schemaContent);
        
        // Match enum definitions: enum EnumName { VALUE1 VALUE2 }
        var enumPattern = @"enum\s+(\w+)\s*\{([^}]+)\}";
        var enumMatches = Regex.Matches(content, enumPattern, RegexOptions.Singleline);
        
        foreach (Match enumMatch in enumMatches)
        {
            var enumName = enumMatch.Groups[1].Value;
            var valuesContent = enumMatch.Groups[2].Value;
            
            var values = valuesContent
                .Split(new[] { '\r', '\n', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            
            enums.Add(new ParsedEnum
            {
                Name = enumName,
                Values = values
            });
        }
        
        return enums;
    }
    
    private List<ParsedField> ParseFields(string fieldsContent)
    {
        var fields = new List<ParsedField>();
        
        // Match field definitions: fieldName: Type or fieldName: [Type] or fieldName(arg: Type): Type
        var fieldPattern = @"(\w+)(?:\([^\)]*\))?\s*:\s*(\[?)(\w+)(\]?)(!?)";
        var fieldMatches = Regex.Matches(fieldsContent, fieldPattern);
        
        foreach (Match fieldMatch in fieldMatches)
        {
            var fieldName = fieldMatch.Groups[1].Value;
            var isList = fieldMatch.Groups[2].Value == "[";
            var typeName = fieldMatch.Groups[3].Value;
            var isListEnd = fieldMatch.Groups[4].Value == "]";
            var isRequired = fieldMatch.Groups[5].Value == "!";
            
            var field = new ParsedField
            {
                Name = fieldName,
                TypeName = typeName,
                IsList = isList && isListEnd,
                IsRequired = isRequired,
                IsRelationship = IsRelationshipType(typeName)
            };
            
            fields.Add(field);
        }
        
        return fields;
    }
    
    private bool IsRelationshipType(string typeName)
    {
        // Scalar types are not relationships
        var scalarTypes = new[] { "ID", "String", "Int", "Float", "Boolean" };
        return !scalarTypes.Contains(typeName);
    }
    
    private string RemoveComments(string content)
    {
        // Remove single-line comments (#)
        content = Regex.Replace(content, @"#[^\n]*", "");
        
        // Remove multi-line comments (though not standard GraphQL)
        content = Regex.Replace(content, @"/\*.*?\*/", "", RegexOptions.Singleline);
        
        return content;
    }
    
    /// <summary>
    /// Converts parsed types to table metadata that can be used to create database tables
    /// </summary>
    public static TableMetadata ToTableMetadata(ParsedType parsedType)
    {
        var columns = new List<ColumnDefinition>();
        
        foreach (var field in parsedType.Fields)
        {
            if (field.IsRelationship)
            {
                // Add the foreign key field(s)
                if (field.IsList)
                {
                    // Many-to-many relationship  
                    // Convert plural field name to singular for FK field (friends -> friendIds, not friendsIds)
                    var singularName = field.Name.EndsWith("s") && field.Name.Length > 1
                        ? field.Name.Substring(0, field.Name.Length - 1)
                        : field.Name;
                    
                    columns.Add(new ColumnDefinition
                    {
                        Name = $"{singularName}Ids",
                        ScalarType = GraphQLScalarType.ID,
                        IsList = true,
                        IsNullable = !field.IsRequired
                    });
                }
                else
                {
                    // Many-to-one relationship
                    columns.Add(new ColumnDefinition
                    {
                        Name = $"{field.Name}Id",
                        ScalarType = GraphQLScalarType.ID,
                        IsNullable = !field.IsRequired
                    });
                }
                
                // Add the relationship definition
                var foreignKeyName = field.IsList 
                    ? (field.Name.EndsWith("s") && field.Name.Length > 1
                        ? $"{field.Name.Substring(0, field.Name.Length - 1)}Ids"
                        : $"{field.Name}Ids")
                    : $"{field.Name}Id";
                
                columns.Add(new ColumnDefinition
                {
                    Name = field.Name,
                    IsList = field.IsList,
                    RelatedTable = field.TypeName,
                    ForeignKey = foreignKeyName,
                    RelationType = field.IsList ? RelationType.ManyToMany : RelationType.ManyToOne
                });
            }
            else
            {
                // Regular scalar field
                columns.Add(new ColumnDefinition
                {
                    Name = field.Name,
                    ScalarType = MapScalarType(field.TypeName),
                    IsList = field.IsList,
                    IsNullable = !field.IsRequired
                });
            }
        }
        
        return new TableMetadata
        {
            Name = parsedType.Name,
            Columns = columns
        };
    }
    
    private static GraphQLScalarType MapScalarType(string typeName)
    {
        return typeName switch
        {
            "ID" => GraphQLScalarType.ID,
            "String" => GraphQLScalarType.String,
            "Int" => GraphQLScalarType.Int,
            "Float" => GraphQLScalarType.Float,
            "Boolean" => GraphQLScalarType.Boolean,
            _ => GraphQLScalarType.String // Default to string for custom types
        };
    }
}

/// <summary>
/// Represents a parsed GraphQL type definition
/// </summary>
public class ParsedType
{
    public string Name { get; set; } = "";
    public List<ParsedField> Fields { get; set; } = new();
}

/// <summary>
/// Represents a parsed field in a GraphQL type
/// </summary>
public class ParsedField
{
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public bool IsList { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRelationship { get; set; }
}

/// <summary>
/// Represents a parsed GraphQL enum definition
/// </summary>
public class ParsedEnum
{
    public string Name { get; set; } = "";
    public List<string> Values { get; set; } = new();
}
