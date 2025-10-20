using Xunit;
using SharpGraph.Core.GraphQL;
using Xunit.Abstractions;

namespace SharpGraph.Tests;

public class MutationParsingDebugTests
{
    private readonly ITestOutputHelper _output;

    public MutationParsingDebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_Mutation_Parsing()
    {
        var source = "mutation { createUser(input: { name: \"Charlie\" }) { id } }";
        
        _output.WriteLine("Source: " + source);
        _output.WriteLine("");
        _output.WriteLine("Tokens:");
        
        var lexer = new GraphQLLexer(source);
        var tokenNum = 0;
        while (true)
        {
            var token = lexer.NextToken();
            _output.WriteLine($"{tokenNum,2}: {token.Type,-15} '{token.Value}' at {token.Line}:{token.Column}");
            tokenNum++;
            if (token.Type == TokenType.EOF) break;
        }
        
        _output.WriteLine("");
        _output.WriteLine("Parsing:");
        
        try
        {
            var parser = new GraphQLParser(source);
            var document = parser.Parse();
            _output.WriteLine("SUCCESS! Parsed without errors.");
            _output.WriteLine($"Document has {document.Definitions.Count} definitions");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"FAILED: {ex.Message}");
        }
    }
}
