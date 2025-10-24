using SharpGraph.Core.GraphQL;
using Xunit;

namespace SharpGraph.Tests;

public class LexerTests
{
    [Fact]
    public void Lexer_Tokenizes_Simple_Query()
    {
        var source = "query { users { id name } }";
        var lexer = new GraphQLLexer(source.AsSpan());
        
        // Can't store ref struct in List, so we verify inline
        var token1 = lexer.NextToken();
        Assert.Equal(TokenType.Query, token1.Type);
        
        var token2 = lexer.NextToken();
        Assert.Equal(TokenType.BraceOpen, token2.Type);
        
        var token3 = lexer.NextToken();
        Assert.Equal(TokenType.Name, token3.Type);
        Assert.Equal("users", token3.Value.ToString());
    }
    
    [Fact]
    public void Lexer_Handles_Strings()
    {
        var source = "\"hello world\"";
        var lexer = new GraphQLLexer(source.AsSpan());
        
        var token = lexer.NextToken();
        
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal("hello world", token.Value.ToString());
    }
    
    [Fact]
    public void Lexer_Handles_Numbers()
    {
        var source = "123 45.67";
        var lexer = new GraphQLLexer(source.AsSpan());
        
        var int_token = lexer.NextToken();
        Assert.Equal(TokenType.Int, int_token.Type);
        Assert.Equal("123", int_token.Value.ToString());
        
        var float_token = lexer.NextToken();
        Assert.Equal(TokenType.Float, float_token.Type);
        Assert.Equal("45.67", float_token.Value.ToString());
    }
    
    [Fact]
    public void Lexer_Handles_Keywords()
    {
        var source = "query mutation subscription fragment";
        var lexer = new GraphQLLexer(source.AsSpan());
        
        Assert.Equal(TokenType.Query, lexer.NextToken().Type);
        Assert.Equal(TokenType.Mutation, lexer.NextToken().Type);
        Assert.Equal(TokenType.Subscription, lexer.NextToken().Type);
        Assert.Equal(TokenType.Fragment, lexer.NextToken().Type);
    }
    
    [Fact]
    public void Lexer_Skips_Comments()
    {
        var source = @"
# This is a comment
query # another comment
{
    users
}";
        var lexer = new GraphQLLexer(source.AsSpan());
        
        var token1 = lexer.NextToken();
        Assert.Equal(TokenType.Query, token1.Type);
        
        var token2 = lexer.NextToken();
        Assert.Equal(TokenType.BraceOpen, token2.Type);
    }
}


