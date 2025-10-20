# Development

## Building from Source

```bash
# Clone repository
git clone https://github.com/your-org/sharpgraph.git
cd sharpgraph

# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Build specific project
dotnet build src/SharpGraph.Core --configuration Release
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "FullyQualifiedName~IndexTests"
dotnet test --filter "FullyQualifiedName~StorageTests"
dotnet test --filter "FullyQualifiedName~GraphQLTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run performance tests
dotnet run --project src/SharpGraph.Benchmark --configuration Release
```

**Test Coverage:**
- ✅ **231 total tests**
- ✅ **100% passing**
- ✅ Coverage across all major components

## Contributing

**Development Environment:**
- Visual Studio 2022 or VS Code
- .NET 9.0 SDK
- Git

**Code Standards:**
- Follow existing code style
- Add unit tests for new features
- Update documentation
- Performance benchmarks for optimization changes

**Pull Request Process:**
1. Fork the repository
2. Create feature branch
3. Add tests and documentation
4. Ensure all tests pass
5. Submit pull request
