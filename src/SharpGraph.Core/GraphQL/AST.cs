using System.Text.Json;

namespace SharpGraph.Core.GraphQL;

/// <summary>
/// GraphQL AST node types
/// </summary>
public abstract class ASTNode
{
}

public class Document : ASTNode
{
    public List<Definition> Definitions { get; set; } = new();
}

public abstract class Definition : ASTNode
{
}

public class OperationDefinition : Definition
{
    public OperationType Operation { get; set; }
    public string? Name { get; set; }
    public List<VariableDefinition> Variables { get; set; } = new();
    public SelectionSet SelectionSet { get; set; } = new();
}

public enum OperationType
{
    Query,
    Mutation,
    Subscription
}

public class VariableDefinition : ASTNode
{
    public string Name { get; set; } = string.Empty;
    public TypeNode Type { get; set; } = new NamedType { Name = "String" };
    public JsonElement? DefaultValue { get; set; }
}

public class SelectionSet : ASTNode
{
    public List<Selection> Selections { get; set; } = new();
}

public abstract class Selection : ASTNode
{
}

public class Field : Selection
{
    public string? Alias { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Argument> Arguments { get; set; } = new();
    public SelectionSet? SelectionSet { get; set; }
}

public class FragmentSpread : Selection
{
    public string Name { get; set; } = string.Empty;
    public List<Directive> Directives { get; set; } = new();
}

public class InlineFragment : Selection
{
    public string? TypeCondition { get; set; }
    public List<Directive> Directives { get; set; } = new();
    public SelectionSet SelectionSet { get; set; } = new();
}

public class FragmentDefinition : Definition
{
    public string Name { get; set; } = string.Empty;
    public string TypeCondition { get; set; } = string.Empty;
    public List<Directive> Directives { get; set; } = new();
    public SelectionSet SelectionSet { get; set; } = new();
}

public class Directive : ASTNode
{
    public string Name { get; set; } = string.Empty;
    public List<Argument> Arguments { get; set; } = new();
}

public class Argument : ASTNode
{
    public string Name { get; set; } = string.Empty;
    public JsonElement Value { get; set; }
}

public abstract class TypeNode : ASTNode
{
}

public class NamedType : TypeNode
{
    public string Name { get; set; } = string.Empty;
}

public class ListType : TypeNode
{
    public TypeNode Type { get; set; } = new NamedType { Name = "String" };
}

public class NonNullType : TypeNode
{
    public TypeNode Type { get; set; } = new NamedType { Name = "String" };
}

public class TypeDefinition : Definition
{
    public string Name { get; set; } = string.Empty;
    public List<FieldDefinition> Fields { get; set; } = new();
}

public class FieldDefinition : ASTNode
{
    public string Name { get; set; } = string.Empty;
    public TypeNode Type { get; set; } = new NamedType { Name = "String" };
    public List<InputValueDefinition> Arguments { get; set; } = new();
}

public class InputValueDefinition : ASTNode
{
    public string Name { get; set; } = string.Empty;
    public TypeNode Type { get; set; } = new NamedType { Name = "String" };
    public JsonElement? DefaultValue { get; set; }
}

