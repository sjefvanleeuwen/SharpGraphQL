using System;

namespace SharpGraph.Core.GraphQL;

/// <summary>
/// GraphQL Lexer implementing the OFFICIAL GraphQL October 2021 Specification
/// Source: https://spec.graphql.org/October2021/
/// 
/// Lexical Grammar from Section 2.1:
/// 
/// Token :: Punctuator | Name | IntValue | FloatValue | StringValue
/// 
/// Punctuator :: one of ! $ & ( ) ... : = @ [ ] { | }
/// 
/// Name :: NameStart NameContinue* [lookahead ∉ NameContinue]
/// NameStart :: Letter | _
/// NameContinue :: Letter | Digit | _
/// Letter :: [A-Za-z]
/// Digit :: [0-9]
/// 
/// IntValue :: IntegerPart [lookahead ∉ {Digit, ., NameStart}]
/// IntegerPart :: NegativeSign? 0 | NegativeSign? NonZeroDigit Digit*
/// NegativeSign :: -
/// NonZeroDigit :: Digit but not 0
/// 
/// FloatValue :: IntegerPart FractionalPart ExponentPart [lookahead ∉ {Digit, ., NameStart}]
///             | IntegerPart FractionalPart [lookahead ∉ {Digit, ., NameStart}]
///             | IntegerPart ExponentPart [lookahead ∉ {Digit, ., NameStart}]
/// FractionalPart :: . Digit+
/// ExponentPart :: ExponentIndicator Sign? Digit+
/// ExponentIndicator :: one of e E
/// Sign :: one of + -
/// 
/// StringValue :: "" [lookahead ≠ "]
///              | " StringCharacter+ "
///              | """ BlockStringCharacter* """
/// 
/// Ignored :: UnicodeBOM | WhiteSpace | LineTerminator | Comment | Comma
/// WhiteSpace :: Horizontal Tab (U+0009) | Space (U+0020)
/// LineTerminator :: New Line (U+000A)
///                 | Carriage Return (U+000D) [lookahead ≠ New Line (U+000A)]
///                 | Carriage Return (U+000D) New Line (U+000A)
/// Comment :: # CommentChar* [lookahead ∉ CommentChar]
/// Comma :: ,
/// </summary>
public ref struct GraphQLLexer
{
    private readonly ReadOnlySpan<char> _source;
    private int _position;
    private int _line;
    private int _column;

    public GraphQLLexer(ReadOnlySpan<char> source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    public bool IsAtEnd => _position >= _source.Length;

    /// <summary>
    /// Returns the next token. Implements Token production from spec.
    /// Token :: Punctuator | Name | IntValue | FloatValue | StringValue
    /// </summary>
    public Token NextToken()
    {
        SkipIgnored();

        if (IsAtEnd)
        {
            return new Token(TokenType.EOF, ReadOnlySpan<char>.Empty, _line, _column);
        }

        var startLine = _line;
        var startColumn = _column;
        var ch = Current;

        // Check for Punctuators (spec section 2.1.8)
        // Punctuator :: one of ! $ & ( ) ... : = @ [ ] { | }
        switch (ch)
        {
            case '!':
                Advance();
                return new Token(TokenType.Bang, "!", startLine, startColumn);
            case '$':
                Advance();
                return new Token(TokenType.Dollar, "$", startLine, startColumn);
            case '&':
                Advance();
                return new Token(TokenType.Ampersand, "&", startLine, startColumn);
            case '(':
                Advance();
                return new Token(TokenType.ParenOpen, "(", startLine, startColumn);
            case ')':
                Advance();
                return new Token(TokenType.ParenClose, ")", startLine, startColumn);
            case ':':
                Advance();
                return new Token(TokenType.Colon, ":", startLine, startColumn);
            case '=':
                Advance();
                return new Token(TokenType.Equals, "=", startLine, startColumn);
            case '@':
                Advance();
                return new Token(TokenType.At, "@", startLine, startColumn);
            case '[':
                Advance();
                return new Token(TokenType.BracketOpen, "[", startLine, startColumn);
            case ']':
                Advance();
                return new Token(TokenType.BracketClose, "]", startLine, startColumn);
            case '{':
                Advance();
                return new Token(TokenType.BraceOpen, "{", startLine, startColumn);
            case '|':
                Advance();
                return new Token(TokenType.Pipe, "|", startLine, startColumn);
            case '}':
                Advance();
                return new Token(TokenType.BraceClose, "}", startLine, startColumn);
            
            // Spread operator ... (three dots)
            case '.':
                if (Peek(1) == '.' && Peek(2) == '.')
                {
                    Advance(3);
                    return new Token(TokenType.Spread, "...", startLine, startColumn);
                }
                throw new GraphQLSyntaxException($"Unexpected character '.' at {startLine}:{startColumn}");
            
            // StringValue (spec section 2.9.4)
            case '"':
                return ReadStringValue(startLine, startColumn);
            
            // IntValue or FloatValue or Negative number (spec section 2.9.1 and 2.9.2)
            case '-':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return ReadNumberValue(startLine, startColumn);
            
            // Name (spec section 2.1.9)
            default:
                if (IsNameStart(ch))
                {
                    return ReadName(startLine, startColumn);
                }
                throw new GraphQLSyntaxException($"Unexpected character '{ch}' at {startLine}:{startColumn}");
        }
    }

    /// <summary>
    /// Skips Ignored tokens per spec section 2.1.7.
    /// Ignored :: UnicodeBOM | WhiteSpace | LineTerminator | Comment | Comma
    /// </summary>
    private void SkipIgnored()
    {
        while (!IsAtEnd)
        {
            var ch = Current;

            // WhiteSpace :: Horizontal Tab (U+0009) | Space (U+0020)
            if (ch == '\t' || ch == ' ')
            {
                Advance();
                continue;
            }

            // LineTerminator :: New Line (U+000A) | Carriage Return (U+000D) ...
            if (ch == '\n')
            {
                _line++;
                _column = 1;
                _position++;
                continue;
            }

            if (ch == '\r')
            {
                _line++;
                _column = 1;
                _position++;
                // Handle \r\n as single line terminator
                if (!IsAtEnd && Current == '\n')
                {
                    _position++;
                }
                continue;
            }

            // Comment :: # CommentChar*
            if (ch == '#')
            {
                SkipComment();
                continue;
            }

            // Comma :: ,
            if (ch == ',')
            {
                Advance();
                continue;
            }

            // UnicodeBOM (U+FEFF)
            if (ch == '\uFEFF')
            {
                Advance();
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// Skips a comment per spec section 2.1.4.
    /// Comment :: # CommentChar* [lookahead ∉ CommentChar]
    /// CommentChar :: SourceCharacter but not LineTerminator
    /// </summary>
    private void SkipComment()
    {
        // Skip the #
        Advance();

        // Read until line terminator or EOF
        while (!IsAtEnd)
        {
            var ch = Current;
            if (ch == '\n' || ch == '\r')
            {
                break;
            }
            Advance();
        }
    }

    /// <summary>
    /// Reads a Name token per spec section 2.1.9.
    /// Name :: NameStart NameContinue* [lookahead ∉ NameContinue]
    /// NameStart :: Letter | _
    /// NameContinue :: Letter | Digit | _
    /// Letter :: [A-Za-z]
    /// </summary>
    private Token ReadName(int startLine, int startColumn)
    {
        var start = _position;

        // Read NameStart
        if (!IsNameStart(Current))
        {
            throw new GraphQLSyntaxException($"Expected name start at {startLine}:{startColumn}");
        }
        Advance();

        // Read NameContinue*
        while (!IsAtEnd && IsNameContinue(Current))
        {
            Advance();
        }

        var text = _source.Slice(start, _position - start).ToString();
        
        // Check for keywords and return appropriate token type
        var tokenType = text switch
        {
            "query" => TokenType.Query,
            "mutation" => TokenType.Mutation,
            "subscription" => TokenType.Subscription,
            "fragment" => TokenType.Fragment,
            "on" => TokenType.On,
            "true" or "false" => TokenType.Boolean,
            "null" => TokenType.Null,
            "type" => TokenType.Type,
            "input" => TokenType.Input,
            "interface" => TokenType.Interface,
            "implements" => TokenType.Implements,
            "enum" => TokenType.Enum,
            "union" => TokenType.Union,
            "scalar" => TokenType.Scalar,
            "schema" => TokenType.Schema,
            "extend" => TokenType.Extend,
            "directive" => TokenType.Directive,
            "repeatable" => TokenType.Repeatable,
            _ => TokenType.Name
        };

        return new Token(tokenType, text, startLine, startColumn);
    }

    /// <summary>
    /// Reads StringValue per spec section 2.9.4.
    /// StringValue :: "" [lookahead ≠ "]
    ///              | " StringCharacter+ "
    ///              | """ BlockStringCharacter* """
    /// </summary>
    private Token ReadStringValue(int startLine, int startColumn)
    {
        var start = _position;

        // Read opening "
        Advance(); // Skip first "

        // Check for block string """ or empty string ""
        if (!IsAtEnd && Current == '"')
        {
            Advance(); // Skip second "
            
            if (!IsAtEnd && Current == '"')
            {
                // Block string """
                Advance(); // Skip third "
                return ReadBlockString(start, startLine, startColumn);
            }
            
            // Empty string ""
            return new Token(TokenType.String, "", startLine, startColumn);
        }

        // Regular string " ... "
        var value = ReadRegularString();
        return new Token(TokenType.String, value, startLine, startColumn);
    }

    /// <summary>
    /// Reads a regular string (not a block string).
    /// StringCharacter :: SourceCharacter but not " or \ or LineTerminator
    ///                  | \u EscapedUnicode
    ///                  | \ EscapedCharacter
    /// </summary>
    private string ReadRegularString()
    {
        var result = "";

        while (!IsAtEnd)
        {
            var ch = Current;

            if (ch == '"')
            {
                Advance(); // Skip closing "
                return result;
            }

            if (ch == '\\')
            {
                Advance();
                if (IsAtEnd)
                {
                    throw new GraphQLSyntaxException($"Unterminated string escape at {_line}:{_column}");
                }

                var escaped = Current;
                Advance();

                // EscapedCharacter :: one of " \ / b f n r t
                result += escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    '/' => '/',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'u' => ReadUnicodeEscape(),
                    _ => throw new GraphQLSyntaxException($"Invalid escape sequence '\\{escaped}' at {_line}:{_column}")
                };
            }
            else if (ch == '\n' || ch == '\r')
            {
                throw new GraphQLSyntaxException($"Unterminated string at {_line}:{_column}");
            }
            else
            {
                result += ch;
                Advance();
            }
        }

        throw new GraphQLSyntaxException($"Unterminated string at {_line}:{_column}");
    }

    /// <summary>
    /// Reads Unicode escape \uXXXX.
    /// </summary>
    private char ReadUnicodeEscape()
    {
        var hex = "";
        for (int i = 0; i < 4; i++)
        {
            if (IsAtEnd || !IsHexDigit(Current))
            {
                throw new GraphQLSyntaxException($"Invalid unicode escape at {_line}:{_column}");
            }
            hex += Current;
            Advance();
        }
        return (char)Convert.ToInt32(hex, 16);
    }

    /// <summary>
    /// Reads a block string """ ... """.
    /// BlockStringCharacter :: SourceCharacter but not """ or \"""
    ///                       | \"""
    /// </summary>
    private Token ReadBlockString(int start, int startLine, int startColumn)
    {
        var content = "";

        while (!IsAtEnd)
        {
            var ch = Current;

            // Check for closing """
            if (ch == '"' && Peek(1) == '"' && Peek(2) == '"')
            {
                Advance(3); // Skip closing """
                
                // Apply block string value processing (spec section 2.9.4 semantics)
                // This removes common indentation and leading/trailing empty lines
                var processedValue = ProcessBlockStringValue(content);
                return new Token(TokenType.String, processedValue, startLine, startColumn);
            }

            // Check for escaped """ -> \"""
            if (ch == '\\' && Peek(1) == '"' && Peek(2) == '"' && Peek(3) == '"')
            {
                content += "\"\"\"";
                Advance(4);
                continue;
            }

            // Track line terminators for block string formatting
            if (ch == '\n')
            {
                content += ch;
                _line++;
                _column = 1;
                _position++;
                continue;
            }

            if (ch == '\r')
            {
                _line++;
                _column = 1;
                _position++;
                // Handle \r\n as single line terminator
                if (!IsAtEnd && Current == '\n')
                {
                    content += "\n";
                    _position++;
                }
                else
                {
                    content += "\n";
                }
                continue;
            }

            content += ch;
            Advance();
        }

        throw new GraphQLSyntaxException($"Unterminated block string at {startLine}:{startColumn}");
    }

    /// <summary>
    /// Process block string value per spec semantics.
    /// Implements BlockStringValue() algorithm from spec section 2.9.4.
    /// </summary>
    private string ProcessBlockStringValue(string raw)
    {
        var lines = raw.Split('\n');
        
        // Find common indentation (skip first line)
        int? commonIndent = null;
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var indent = 0;
            while (indent < line.Length && (line[indent] == ' ' || line[indent] == '\t'))
            {
                indent++;
            }
            
            if (indent < line.Length) // Non-empty line
            {
                if (commonIndent == null || indent < commonIndent)
                {
                    commonIndent = indent;
                }
            }
        }

        // Remove common indentation
        if (commonIndent.HasValue)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Length >= commonIndent.Value)
                {
                    lines[i] = lines[i].Substring(commonIndent.Value);
                }
            }
        }

        // Remove leading empty lines
        var startIndex = 0;
        while (startIndex < lines.Length && string.IsNullOrWhiteSpace(lines[startIndex]))
        {
            startIndex++;
        }

        // Remove trailing empty lines
        var endIndex = lines.Length - 1;
        while (endIndex >= 0 && string.IsNullOrWhiteSpace(lines[endIndex]))
        {
            endIndex--;
        }

        // Join remaining lines
        if (startIndex > endIndex)
        {
            return "";
        }

        return string.Join("\n", lines[startIndex..(endIndex + 1)]);
    }

    /// <summary>
    /// Reads IntValue or FloatValue per spec sections 2.9.1 and 2.9.2.
    /// 
    /// IntValue :: IntegerPart [lookahead ∉ {Digit, ., NameStart}]
    /// 
    /// FloatValue :: IntegerPart FractionalPart ExponentPart [lookahead ∉ {Digit, ., NameStart}]
    ///             | IntegerPart FractionalPart [lookahead ∉ {Digit, ., NameStart}]
    ///             | IntegerPart ExponentPart [lookahead ∉ {Digit, ., NameStart}]
    /// 
    /// IntegerPart :: NegativeSign? 0 | NegativeSign? NonZeroDigit Digit*
    /// FractionalPart :: . Digit+
    /// ExponentPart :: ExponentIndicator Sign? Digit+
    /// </summary>
    private Token ReadNumberValue(int startLine, int startColumn)
    {
        var start = _position;
        var isFloat = false;

        // Read NegativeSign?
        if (Current == '-')
        {
            Advance();
        }

        if (IsAtEnd || !char.IsDigit(Current))
        {
            throw new GraphQLSyntaxException($"Expected digit after '-' at {_line}:{_column}");
        }

        // Read IntegerPart
        if (Current == '0')
        {
            Advance();
        }
        else
        {
            // NonZeroDigit Digit*
            while (!IsAtEnd && char.IsDigit(Current))
            {
                Advance();
            }
        }

        // Check for FractionalPart :: . Digit+
        if (!IsAtEnd && Current == '.')
        {
            var next = Peek(1);
            if (next >= '0' && next <= '9')
            {
                isFloat = true;
                Advance(); // Skip .
                
                // Read Digit+
                while (!IsAtEnd && char.IsDigit(Current))
                {
                    Advance();
                }
            }
        }

        // Check for ExponentPart :: ExponentIndicator Sign? Digit+
        if (!IsAtEnd && (Current == 'e' || Current == 'E'))
        {
            isFloat = true;
            Advance(); // Skip e/E

            // Read Sign?
            if (!IsAtEnd && (Current == '+' || Current == '-'))
            {
                Advance();
            }

            // Read Digit+
            if (IsAtEnd || !char.IsDigit(Current))
            {
                throw new GraphQLSyntaxException($"Expected digit in exponent at {_line}:{_column}");
            }

            while (!IsAtEnd && char.IsDigit(Current))
            {
                Advance();
            }
        }

        // Lookahead check: must not be followed by Digit, ., or NameStart
        if (!IsAtEnd)
        {
            var next = Current;
            if (char.IsDigit(next) || next == '.' || IsNameStart(next))
            {
                throw new GraphQLSyntaxException($"Invalid number format at {_line}:{_column}");
            }
        }

        var text = _source.Slice(start, _position - start).ToString();
        var tokenType = isFloat ? TokenType.Float : TokenType.Int;
        return new Token(tokenType, text, startLine, startColumn);
    }

    // Helper methods

    private char Current => IsAtEnd ? '\0' : _source[_position];

    private char Peek(int offset)
    {
        var pos = _position + offset;
        return pos >= _source.Length ? '\0' : _source[pos];
    }

    private void Advance(int count = 1)
    {
        for (int i = 0; i < count && !IsAtEnd; i++)
        {
            _position++;
            _column++;
        }
    }

    /// <summary>
    /// NameStart :: Letter | _
    /// Letter :: [A-Za-z]
    /// </summary>
    private static bool IsNameStart(char ch)
    {
        return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';
    }

    /// <summary>
    /// NameContinue :: Letter | Digit | _
    /// </summary>
    private static bool IsNameContinue(char ch)
    {
        return IsNameStart(ch) || (ch >= '0' && ch <= '9');
    }

    private static bool IsHexDigit(char ch)
    {
        return (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
    }
}

public enum TokenType
{
    // End of file
    EOF,

    // Punctuators (spec section 2.1.8)
    Bang,           // !
    Dollar,         // $
    Ampersand,      // &
    ParenOpen,      // (
    ParenClose,     // )
    Spread,         // ...
    Colon,          // :
    Equals,         // =
    At,             // @
    BracketOpen,    // [
    BracketClose,   // ]
    BraceOpen,      // {
    Pipe,           // |
    BraceClose,     // }

    // Literals
    Name,
    Int,
    Float,
    String,
    Boolean,
    Null,

    // Keywords (returned as special Name tokens)
    Query,
    Mutation,
    Subscription,
    Fragment,
    On,
    Type,
    Input,
    Interface,
    Implements,
    Enum,
    Union,
    Scalar,
    Schema,
    Extend,
    Directive,
    Repeatable
}

public readonly ref struct Token
{
    public readonly TokenType Type;
    public readonly string Value;
    public readonly int Line;
    public readonly int Column;

    public Token(TokenType type, string value, int line, int column)
    {
        Type = type;
        Value = value;
        Line = line;
        Column = column;
    }

    public Token(TokenType type, ReadOnlySpan<char> value, int line, int column)
    {
        Type = type;
        Value = value.ToString();
        Line = line;
        Column = column;
    }

    public override string ToString() => $"{Type} '{Value}' at {Line}:{Column}";
}

public class GraphQLSyntaxException : Exception
{
    public GraphQLSyntaxException(string message) : base(message) { }
}

