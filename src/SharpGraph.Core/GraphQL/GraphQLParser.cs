using System.Text.Json;
using System.Collections.Generic;

namespace SharpGraph.Core.GraphQL;

/// <summary>
/// Stored token for parser (non-ref struct version of Token)
/// </summary>
public readonly struct StoredToken
{
    public TokenType Type { get; init; }
    public string Value { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
}

/// <summary>
/// Recursive descent parser for GraphQL using the spec-compliant lexer.
/// Parses tokens from GraphQLLexer into AST.
/// </summary>
public class GraphQLParser
{
    private readonly string _source;
    private readonly List<StoredToken> _tokens;
    private int _tokenIndex;
    private StoredToken _currentToken;
    
    public GraphQLParser(string source)
    {
        _source = source;
        _tokens = new List<StoredToken>();
        
        // Pre-tokenize the entire source using the spec-compliant lexer
        var lexer = new GraphQLLexer(source);
        while (true)
        {
            var token = lexer.NextToken();
            // Copy token data to non-ref struct for storage
            var storedToken = new StoredToken
            {
                Type = token.Type,
                Value = token.Value.ToString(),
                Line = token.Line,
                Column = token.Column
            };
            _tokens.Add(storedToken);
            if (token.Type == TokenType.EOF) break;
        }
        
        _tokenIndex = 0;
        _currentToken = _tokens[0];
    }
    
    private TokenType CurrentType => _currentToken.Type;
    private string CurrentValue => _currentToken.Value;
    private int CurrentLine => _currentToken.Line;
    private int CurrentColumn => _currentToken.Column;
    
    public Document Parse()
    {
        var document = new Document();
        
        while (CurrentType != TokenType.EOF)
        {
            document.Definitions.Add(ParseDefinition());
        }
        
        return document;
    }
    
    private Definition ParseDefinition()
    {
        return CurrentType switch
        {
            TokenType.Query or TokenType.Mutation or TokenType.Subscription => ParseOperationDefinition(),
            TokenType.BraceOpen => ParseOperationDefinition(),
            TokenType.Fragment => ParseFragmentDefinition(),
            TokenType.Type => ParseTypeDefinition(),
            _ => throw new GraphQLSyntaxException($"Unexpected token: {CurrentType}")
        };
    }
    
    private OperationDefinition ParseOperationDefinition()
    {
        var operation = new OperationDefinition();
        
        if (CurrentType == TokenType.Query || CurrentType == TokenType.Mutation || CurrentType == TokenType.Subscription)
        {
            operation.Operation = CurrentType switch
            {
                TokenType.Query => OperationType.Query,
                TokenType.Mutation => OperationType.Mutation,
                TokenType.Subscription => OperationType.Subscription,
                _ => OperationType.Query
            };
            Advance();
            
            if (CurrentType == TokenType.Name)
            {
                operation.Name = CurrentValue;
                Advance();
            }
            
            if (CurrentType == TokenType.ParenOpen)
            {
                operation.Variables = ParseVariableDefinitions();
            }
        }
        else
        {
            operation.Operation = OperationType.Query;
        }
        
        operation.SelectionSet = ParseSelectionSet();
        return operation;
    }
    
    private List<VariableDefinition> ParseVariableDefinitions()
    {
        var variables = new List<VariableDefinition>();
        Expect(TokenType.ParenOpen);
        
        while (CurrentType != TokenType.ParenClose && CurrentType != TokenType.EOF)
        {
            variables.Add(ParseVariableDefinition());
            // Commas are ignored tokens per spec, automatically skipped by lexer
        }
        
        Expect(TokenType.ParenClose);
        return variables;
    }
    
    private VariableDefinition ParseVariableDefinition()
    {
        var variable = new VariableDefinition();
        Expect(TokenType.Dollar);
        variable.Name = ExpectName();
        Expect(TokenType.Colon);
        variable.Type = ParseType();
        
        if (CurrentType == TokenType.Equals)
        {
            Advance();
            variable.DefaultValue = ParseValue();
        }
        
        return variable;
    }
    
    private SelectionSet ParseSelectionSet()
    {
        var selectionSet = new SelectionSet();
        Expect(TokenType.BraceOpen);
        
        while (CurrentType != TokenType.BraceClose && CurrentType != TokenType.EOF)
        {
            selectionSet.Selections.Add(ParseSelection());
        }
        
        Expect(TokenType.BraceClose);
        return selectionSet;
    }
    
    private Selection ParseSelection()
    {
        // Check for spread (...) which indicates fragment spread or inline fragment
        if (CurrentType == TokenType.Spread)
        {
            Advance();
            
            // Inline fragment: ... on TypeName { ... }
            if (CurrentType == TokenType.On)
            {
                return ParseInlineFragment();
            }
            
            // Fragment spread: ...FragmentName
            return ParseFragmentSpread();
        }
        
        // Regular field
        return ParseField();
    }
    
    private FragmentSpread ParseFragmentSpread()
    {
        var fragmentSpread = new FragmentSpread
        {
            Name = ExpectName()
        };
        
        // Parse directives if present
        while (CurrentType == TokenType.At)
        {
            fragmentSpread.Directives.Add(ParseDirective());
        }
        
        return fragmentSpread;
    }
    
    private InlineFragment ParseInlineFragment()
    {
        Expect(TokenType.On);
        
        var inlineFragment = new InlineFragment
        {
            TypeCondition = ExpectName()
        };
        
        // Parse directives if present
        while (CurrentType == TokenType.At)
        {
            inlineFragment.Directives.Add(ParseDirective());
        }
        
        inlineFragment.SelectionSet = ParseSelectionSet();
        
        return inlineFragment;
    }
    
    private FragmentDefinition ParseFragmentDefinition()
    {
        Expect(TokenType.Fragment);
        
        var fragmentDef = new FragmentDefinition
        {
            Name = ExpectName()
        };
        
        Expect(TokenType.On);
        fragmentDef.TypeCondition = ExpectName();
        
        // Parse directives if present
        while (CurrentType == TokenType.At)
        {
            fragmentDef.Directives.Add(ParseDirective());
        }
        
        fragmentDef.SelectionSet = ParseSelectionSet();
        
        return fragmentDef;
    }
    
    private Directive ParseDirective()
    {
        Expect(TokenType.At);
        
        var directive = new Directive
        {
            Name = ExpectName()
        };
        
        // Parse arguments if present
        if (CurrentType == TokenType.ParenOpen)
        {
            directive.Arguments = ParseArguments();
        }
        
        return directive;
    }
    
    private Field ParseField()
    {
        var field = new Field();
        var name = ExpectName();
        
        if (CurrentType == TokenType.Colon)
        {
            field.Alias = name;
            Advance();
            field.Name = ExpectName();
        }
        else
        {
            field.Name = name;
        }
        
        if (CurrentType == TokenType.ParenOpen)
        {
            field.Arguments = ParseArguments();
        }
        
        if (CurrentType == TokenType.BraceOpen)
        {
            field.SelectionSet = ParseSelectionSet();
        }
        
        return field;
    }
    
    private List<Argument> ParseArguments()
    {
        var arguments = new List<Argument>();
        Expect(TokenType.ParenOpen);
        
        while (CurrentType != TokenType.ParenClose && CurrentType != TokenType.EOF)
        {
            arguments.Add(ParseArgument());
            // Commas are ignored tokens per spec, automatically skipped by lexer
        }
        
        Expect(TokenType.ParenClose);
        return arguments;
    }
    
    private Argument ParseArgument()
    {
        var argument = new Argument { Name = ExpectName() };
        Expect(TokenType.Colon);
        argument.Value = ParseValue();
        return argument;
    }
    
    private JsonElement ParseValue()
    {
        return CurrentType switch
        {
            TokenType.String => ParseStringValue(),
            TokenType.Int => ParseIntValue(),
            TokenType.Float => ParseFloatValue(),
            TokenType.Boolean => ParseBooleanValue(),
            TokenType.Null => ParseNullValue(),
            TokenType.BracketOpen => ParseListValue(),
            TokenType.BraceOpen => ParseObjectValue(),
            TokenType.Dollar => ParseVariableValue(),
            TokenType.Name => ParseEnumValue(),  // Handle enum values (bare identifiers)
            _ => throw new GraphQLSyntaxException($"Unexpected value type: {CurrentType}")
        };
    }
    
    private JsonElement ParseStringValue()
    {
        var value = CurrentValue;
        Advance();
        return JsonDocument.Parse($"\"{value}\"").RootElement;
    }
    
    private JsonElement ParseIntValue()
    {
        var value = CurrentValue;
        Advance();
        return JsonDocument.Parse(value).RootElement;
    }
    
    private JsonElement ParseFloatValue()
    {
        var value = CurrentValue;
        Advance();
        return JsonDocument.Parse(value).RootElement;
    }
    
    private JsonElement ParseBooleanValue()
    {
        var value = CurrentValue;
        Advance();
        return JsonDocument.Parse(value).RootElement;
    }
    
    private JsonElement ParseNullValue()
    {
        Advance();
        return JsonDocument.Parse("null").RootElement;
    }
    
    private JsonElement ParseEnumValue()
    {
        // Enum values are bare identifiers, returned as strings
        var value = CurrentValue;
        Advance();
        return JsonDocument.Parse($"\"{value}\"").RootElement;
    }
    
    private JsonElement ParseListValue()
    {
        var values = new List<JsonElement>();
        Expect(TokenType.BracketOpen);
        
        while (CurrentType != TokenType.BracketClose && CurrentType != TokenType.EOF)
        {
            values.Add(ParseValue());
            // Commas are ignored tokens per spec, automatically skipped by lexer
        }
        
        Expect(TokenType.BracketClose);
        var json = JsonSerializer.Serialize(values);
        return JsonDocument.Parse(json).RootElement;
    }
    
    private JsonElement ParseObjectValue()
    {
        var obj = new Dictionary<string, JsonElement>();
        Expect(TokenType.BraceOpen);
        
        while (CurrentType != TokenType.BraceClose && CurrentType != TokenType.EOF)
        {
            var name = ExpectName();
            Expect(TokenType.Colon);
            var value = ParseValue();
            obj[name] = value;
            // Commas are ignored tokens per spec, automatically skipped by lexer
        }
        
        Expect(TokenType.BraceClose);
        var json = JsonSerializer.Serialize(obj);
        return JsonDocument.Parse(json).RootElement;
    }
    
    private JsonElement ParseVariableValue()
    {
        Expect(TokenType.Dollar);
        var name = ExpectName();
        var json = $"{{\"__variable__\":\"{name}\"}}";
        return JsonDocument.Parse(json).RootElement;
    }
    
    private TypeNode ParseType()
    {
        TypeNode type;
        
        if (CurrentType == TokenType.BracketOpen)
        {
            Advance();
            type = new ListType { Type = ParseType() };
            Expect(TokenType.BracketClose);
        }
        else
        {
            type = new NamedType { Name = ExpectName() };
        }
        
        if (CurrentType == TokenType.Bang)
        {
            Advance();
            type = new NonNullType { Type = type };
        }
        
        return type;
    }
    
    private TypeDefinition ParseTypeDefinition()
    {
        Expect(TokenType.Type);
        var typeDef = new TypeDefinition { Name = ExpectName() };
        Expect(TokenType.BraceOpen);
        
        while (CurrentType != TokenType.BraceClose && CurrentType != TokenType.EOF)
        {
            typeDef.Fields.Add(ParseFieldDefinition());
        }
        
        Expect(TokenType.BraceClose);
        return typeDef;
    }
    
    private FieldDefinition ParseFieldDefinition()
    {
        var fieldDef = new FieldDefinition { Name = ExpectName() };
        
        if (CurrentType == TokenType.ParenOpen)
        {
            fieldDef.Arguments = ParseInputValueDefinitions();
        }
        
        Expect(TokenType.Colon);
        fieldDef.Type = ParseType();
        return fieldDef;
    }
    
    private List<InputValueDefinition> ParseInputValueDefinitions()
    {
        var inputs = new List<InputValueDefinition>();
        Expect(TokenType.ParenOpen);
        
        while (CurrentType != TokenType.ParenClose && CurrentType != TokenType.EOF)
        {
            var input = new InputValueDefinition { Name = ExpectName() };
            Expect(TokenType.Colon);
            input.Type = ParseType();
            
            if (CurrentType == TokenType.Equals)
            {
                Advance();
                input.DefaultValue = ParseValue();
            }
            
            inputs.Add(input);
            // Commas are ignored tokens per spec, automatically skipped by lexer
        }
        
        Expect(TokenType.ParenClose);
        return inputs;
    }
    
    private void Advance()
    {
        if (_tokenIndex < _tokens.Count - 1)
        {
            _tokenIndex++;
            _currentToken = _tokens[_tokenIndex];
        }
    }
    
    private void Expect(TokenType type)
    {
        if (CurrentType != type)
            throw new GraphQLSyntaxException($"Expected {type} but got {CurrentType} at {CurrentLine}:{CurrentColumn}");
        Advance();
    }
    
    private string ExpectName()
    {
        // In GraphQL, keywords can be used as names in certain contexts (like field/argument names)
        // So we accept Name OR keywords that are valid as field/argument names
        // This is especially important for introspection queries which use fields like "type", "query", etc.
        if (CurrentType == TokenType.Name || 
            CurrentType == TokenType.Query ||
            CurrentType == TokenType.Mutation ||
            CurrentType == TokenType.Subscription ||
            CurrentType == TokenType.Type ||
            CurrentType == TokenType.Input || 
            CurrentType == TokenType.On ||
            CurrentType == TokenType.Fragment)
        {
            var name = CurrentValue;
            Advance();
            return name;
        }
        
        throw new GraphQLSyntaxException($"Expected name but got {CurrentType} at {CurrentLine}:{CurrentColumn}");
    }
}

