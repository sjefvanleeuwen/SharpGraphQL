using Xunit;
using SharpGraph.Core.GraphQL;

namespace SharpGraph.Tests;

/// <summary>
/// Comprehensive tests for GraphQLLexer conforming to GraphQL October 2021 specification
/// Tests all token types, edge cases, and error conditions per spec sections 2.1.x
/// </summary>
public class GraphQLLexerTests
{
    #region Punctuator Tests (Spec 2.1.8)

    [Fact]
    public void Lexer_Should_Tokenize_All_Punctuators()
    {
        var source = "! $ & ( ) ... : = @ [ ] { | }";
        var lexer = new GraphQLLexer(source);

        Assert.Equal(TokenType.Bang, lexer.NextToken().Type);
        Assert.Equal(TokenType.Dollar, lexer.NextToken().Type);
        Assert.Equal(TokenType.Ampersand, lexer.NextToken().Type);
        Assert.Equal(TokenType.ParenOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.ParenClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.Spread, lexer.NextToken().Type);
        Assert.Equal(TokenType.Colon, lexer.NextToken().Type);
        Assert.Equal(TokenType.Equals, lexer.NextToken().Type);
        Assert.Equal(TokenType.At, lexer.NextToken().Type);
        Assert.Equal(TokenType.BracketOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.BracketClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.Pipe, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.EOF, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Spread_Operator()
    {
        var source = "...";
        var lexer = new GraphQLLexer(source);
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Spread, token.Type);
        Assert.Equal("...", token.Value);
    }

    [Fact]
    public void Lexer_Should_Reject_Single_Dot()
    {
        var source = ".";
        var lexer = new GraphQLLexer(source);
        
        try
        {
            lexer.NextToken();
            Assert.Fail("Expected GraphQLSyntaxException");
        }
        catch (GraphQLSyntaxException)
        {
            // Expected
        }
    }

    [Fact]
    public void Lexer_Should_Reject_Two_Dots()
    {
        var source = "..";
        var lexer = new GraphQLLexer(source);
        
        try
        {
            lexer.NextToken();
            Assert.Fail("Expected GraphQLSyntaxException");
        }
        catch (GraphQLSyntaxException)
        {
            // Expected
        }
    }

    #endregion

    #region Name Tests (Spec 2.1.9)

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("_hello", "_hello")]
    [InlineData("Hello", "Hello")]
    [InlineData("HELLO", "HELLO")]
    [InlineData("hello123", "hello123")]
    [InlineData("_123", "_123")]
    [InlineData("hello_world", "hello_world")]
    [InlineData("camelCase", "camelCase")]
    [InlineData("PascalCase", "PascalCase")]
    [InlineData("SCREAMING_SNAKE_CASE", "SCREAMING_SNAKE_CASE")]
    public void Lexer_Should_Tokenize_Valid_Names(string source, string expected)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData("query", TokenType.Query)]
    [InlineData("mutation", TokenType.Mutation)]
    [InlineData("subscription", TokenType.Subscription)]
    [InlineData("fragment", TokenType.Fragment)]
    [InlineData("on", TokenType.On)]
    [InlineData("type", TokenType.Type)]
    [InlineData("input", TokenType.Input)]
    [InlineData("interface", TokenType.Interface)]
    [InlineData("implements", TokenType.Implements)]
    [InlineData("enum", TokenType.Enum)]
    [InlineData("union", TokenType.Union)]
    [InlineData("scalar", TokenType.Scalar)]
    [InlineData("schema", TokenType.Schema)]
    [InlineData("extend", TokenType.Extend)]
    [InlineData("directive", TokenType.Directive)]
    [InlineData("repeatable", TokenType.Repeatable)]
    [InlineData("true", TokenType.Boolean)]
    [InlineData("false", TokenType.Boolean)]
    [InlineData("null", TokenType.Null)]
    public void Lexer_Should_Recognize_Keywords(string source, TokenType expectedType)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(expectedType, token.Type);
        Assert.Equal(source, token.Value);
    }

    #endregion

    #region IntValue Tests (Spec 2.9.1)

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "1")]
    [InlineData("123", "123")]
    [InlineData("999", "999")]
    [InlineData("-1", "-1")]
    [InlineData("-123", "-123")]
    [InlineData("2147483647", "2147483647")] // Max int32
    public void Lexer_Should_Tokenize_Valid_Integers(string source, string expected)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Int, token.Type);
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData("00")] // Leading zero not allowed
    [InlineData("01")] // Leading zero not allowed
    [InlineData("0123")] // Leading zero not allowed
    public void Lexer_Should_Reject_Invalid_Integers(string source)
    {
        var lexer = new GraphQLLexer(source);
        
        try
        {
            lexer.NextToken();
            Assert.Fail($"Expected GraphQLSyntaxException for: {source}");
        }
        catch (GraphQLSyntaxException)
        {
            // Expected
        }
    }

    #endregion

    #region FloatValue Tests (Spec 2.9.2)

    [Theory]
    [InlineData("1.0", "1.0")]
    [InlineData("1.5", "1.5")]
    [InlineData("123.456", "123.456")]
    [InlineData("-1.5", "-1.5")]
    [InlineData("0.123", "0.123")]
    [InlineData("1e10", "1e10")]
    [InlineData("1E10", "1E10")]
    [InlineData("1e+10", "1e+10")]
    [InlineData("1e-10", "1e-10")]
    [InlineData("1.5e10", "1.5e10")]
    [InlineData("1.5E+10", "1.5E+10")]
    [InlineData("1.5e-10", "1.5e-10")]
    [InlineData("-1.5e10", "-1.5e10")]
    public void Lexer_Should_Tokenize_Valid_Floats(string source, string expected)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Float, token.Type);
        Assert.Equal(expected, token.Value);
    }

    [Theory]
    [InlineData("1.")] // Missing fractional part
    [InlineData("1e")] // Missing exponent
    [InlineData("1e+")] // Missing exponent digits
    [InlineData("1e-")] // Missing exponent digits
    public void Lexer_Should_Reject_Invalid_Floats(string source)
    {
        var lexer = new GraphQLLexer(source);
        
        try
        {
            lexer.NextToken();
            Assert.Fail($"Expected GraphQLSyntaxException for: {source}");
        }
        catch (GraphQLSyntaxException)
        {
            // Expected
        }
    }

    #endregion

    #region StringValue Tests (Spec 2.9.4)

    [Fact]
    public void Lexer_Should_Tokenize_Empty_String()
    {
        var source = "\"\"";
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("", token.Value);
    }

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"hello world\"", "hello world")]
    [InlineData("\"123\"", "123")]
    [InlineData("\"hello\\nworld\"", "hello\nworld")]
    [InlineData("\"hello\\tworld\"", "hello\tworld")]
    [InlineData("\"\\\"quoted\\\"\"", "\"quoted\"")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\/\"", "/")]
    [InlineData("\"\\b\"", "\b")]
    [InlineData("\"\\f\"", "\f")]
    [InlineData("\"\\r\"", "\r")]
    public void Lexer_Should_Tokenize_Valid_Strings(string source, string expected)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal(expected, token.Value);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Unicode_Escape()
    {
        var source = "\"\\u0048\\u0065\\u006C\\u006C\\u006F\""; // "Hello"
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("Hello", token.Value);
    }

    [Theory]
    [InlineData("\"unterminated")]
    [InlineData("\"line\nbreak\"")]
    [InlineData("\"\\x\"")]
    public void Lexer_Should_Reject_Invalid_Strings(string source)
    {
        var lexer = new GraphQLLexer(source);
        
        try
        {
            lexer.NextToken();
            Assert.Fail($"Expected GraphQLSyntaxException for: {source}");
        }
        catch (GraphQLSyntaxException)
        {
            // Expected
        }
    }

    [Fact]
    public void Lexer_Should_Tokenize_Empty_Block_String()
    {
        var source = "\"\"\"\"\"\"";
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("", token.Value);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Block_String()
    {
        var source = "\"\"\"hello world\"\"\"";
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("hello world", token.Value);
    }

    [Fact]
    public void Lexer_Should_Handle_Block_String_With_Quotes()
    {
        var source = "\"\"\"Contains \" quotes\"\"\"";
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("Contains \" quotes", token.Value);
    }

    [Fact]
    public void Lexer_Should_Handle_Block_String_Escaped_Quotes()
    {
        var source = "\"\"\"Contains \\\"\"\" escaped\"\"\"";
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("Contains \"\"\" escaped", token.Value);
    }

    [Fact]
    public void Lexer_Should_Handle_Block_String_With_Newlines()
    {
        var source = "\"\"\"\nline1\nline2\n\"\"\"";
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("line1\nline2", token.Value); // Leading/trailing empty lines removed
    }

    #endregion

    #region Ignored Tokens Tests (Spec 2.1.7)

    [Theory]
    [InlineData(" hello", "hello")] // Space
    [InlineData("\thello", "hello")] // Tab
    [InlineData("  \t  hello", "hello")] // Multiple whitespace
    public void Lexer_Should_Skip_Whitespace(string source, string expectedValue)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal(expectedValue, token.Value);
    }

    [Theory]
    [InlineData("\nhello", "hello")]
    [InlineData("\rhello", "hello")]
    [InlineData("\r\nhello", "hello")]
    [InlineData("\n\n\nhello", "hello")]
    public void Lexer_Should_Skip_Line_Terminators(string source, string expectedValue)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal(expectedValue, token.Value);
    }

    [Theory]
    [InlineData("# comment\nhello", "hello")]
    [InlineData("#comment\nhello", "hello")]
    [InlineData("# this is a comment\nhello", "hello")]
    [InlineData("#\nhello", "hello")]
    public void Lexer_Should_Skip_Comments(string source, string expectedValue)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal(expectedValue, token.Value);
    }

    [Theory]
    [InlineData(",hello", "hello")]
    [InlineData(",,hello", "hello")]
    [InlineData(" , , hello", "hello")]
    public void Lexer_Should_Skip_Commas(string source, string expectedValue)
    {
        var lexer = new GraphQLLexer(source);
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal(expectedValue, token.Value);
    }

    [Fact]
    public void Lexer_Should_Track_Line_Numbers()
    {
        var source = "hello\nworld\ntest";
        var lexer = new GraphQLLexer(source);
        
        var token1 = lexer.NextToken();
        Assert.Equal(1, token1.Line);
        
        var token2 = lexer.NextToken();
        Assert.Equal(2, token2.Line);
        
        var token3 = lexer.NextToken();
        Assert.Equal(3, token3.Line);
    }

    #endregion

    #region Complete Query Tests

    [Fact]
    public void Lexer_Should_Tokenize_Simple_Query()
    {
        var source = "{ users { id name } }";
        var lexer = new GraphQLLexer(source);
        
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.Name, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.Name, lexer.NextToken().Type);
        Assert.Equal(TokenType.Name, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.EOF, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Query_With_Arguments()
    {
        var source = "query { user(id: 123) { name } }";
        var lexer = new GraphQLLexer(source);
        
        Assert.Equal(TokenType.Query, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("user", token.Value);
        
        Assert.Equal(TokenType.ParenOpen, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("id", token.Value);
        
        Assert.Equal(TokenType.Colon, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Int, token.Type);
        Assert.Equal("123", token.Value);
        
        Assert.Equal(TokenType.ParenClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("name", token.Value);
        
        Assert.Equal(TokenType.BraceClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.EOF, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Mutation_With_Input_Object()
    {
        var source = "mutation { createUser(input: { name: \"Charlie\" }) { id } }";
        var lexer = new GraphQLLexer(source);
        
        // Manually extract tokens
        var t0 = lexer.NextToken();
        var t1 = lexer.NextToken();
        var t2 = lexer.NextToken();
        var t3 = lexer.NextToken();
        var t4 = lexer.NextToken();
        var t5 = lexer.NextToken();
        var t6 = lexer.NextToken();
        var t7 = lexer.NextToken();
        var t8 = lexer.NextToken();
        var t9 = lexer.NextToken();
        var t10 = lexer.NextToken();
        var t11 = lexer.NextToken();
        var t12 = lexer.NextToken();
        var t13 = lexer.NextToken();
        var t14 = lexer.NextToken();
        var t15 = lexer.NextToken();
        var t16 = lexer.NextToken();

        // Verify critical tokens
        Assert.Equal(TokenType.Mutation, t0.Type);
        Assert.Equal(TokenType.BraceOpen, t1.Type);
        Assert.Equal(TokenType.Name, t2.Type);
        Assert.Equal("createUser", t2.Value);
        Assert.Equal(TokenType.ParenOpen, t3.Type);
        Assert.Equal(TokenType.Input, t4.Type); // "input" should be Input keyword
        Assert.Equal("input", t4.Value);
        Assert.Equal(TokenType.Colon, t5.Type);
        Assert.Equal(TokenType.BraceOpen, t6.Type);
        Assert.Equal(TokenType.Name, t7.Type);
        Assert.Equal("name", t7.Value);
        Assert.Equal(TokenType.Colon, t8.Type);
        Assert.Equal(TokenType.String, t9.Type);
        Assert.Equal("Charlie", t9.Value);
        Assert.Equal(TokenType.BraceClose, t10.Type);
        Assert.Equal(TokenType.ParenClose, t11.Type);
        Assert.Equal(TokenType.BraceOpen, t12.Type);
        Assert.Equal(TokenType.Name, t13.Type);
        Assert.Equal("id", t13.Value);
        Assert.Equal(TokenType.BraceClose, t14.Type);
        Assert.Equal(TokenType.BraceClose, t15.Type);
        Assert.Equal(TokenType.EOF, t16.Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Fragment()
    {
        var source = "fragment userFields on User { id name }";
        var lexer = new GraphQLLexer(source);
        
        Assert.Equal(TokenType.Fragment, lexer.NextToken().Type);
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("userFields", token.Value);
        
        Assert.Equal(TokenType.On, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("User", token.Value);
        
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.Name, lexer.NextToken().Type);
        Assert.Equal(TokenType.Name, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceClose, lexer.NextToken().Type);
        Assert.Equal(TokenType.EOF, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Variables()
    {
        var source = "query($id: Int!) { user(id: $id) { name } }";
        var lexer = new GraphQLLexer(source);
        
        Assert.Equal(TokenType.Query, lexer.NextToken().Type);
        Assert.Equal(TokenType.ParenOpen, lexer.NextToken().Type);
        Assert.Equal(TokenType.Dollar, lexer.NextToken().Type);
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("id", token.Value);
        
        Assert.Equal(TokenType.Colon, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("Int", token.Value);
        
        Assert.Equal(TokenType.Bang, lexer.NextToken().Type);
        Assert.Equal(TokenType.ParenClose, lexer.NextToken().Type);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Directives()
    {
        var source = "query { user @include(if: true) { name } }";
        var lexer = new GraphQLLexer(source);
        
        Assert.Equal(TokenType.Query, lexer.NextToken().Type);
        Assert.Equal(TokenType.BraceOpen, lexer.NextToken().Type);
        
        var token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("user", token.Value);
        
        Assert.Equal(TokenType.At, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("include", token.Value);
        
        Assert.Equal(TokenType.ParenOpen, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Name, token.Type);
        Assert.Equal("if", token.Value);
        
        Assert.Equal(TokenType.Colon, lexer.NextToken().Type);
        
        token = lexer.NextToken();
        Assert.Equal(TokenType.Boolean, token.Type);
        Assert.Equal("true", token.Value);
        
        Assert.Equal(TokenType.ParenClose, lexer.NextToken().Type);
    }

    #endregion
}


