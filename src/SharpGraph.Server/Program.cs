using System.Net;
using System.Text;
using System.Text.Json;
using SharpGraph.Core;
using SharpGraph.Core.GraphQL;
using SharpGraph.Core.Storage;

namespace SharpGraph.Server;

class Program
{
    private static SchemaLoader? _schemaLoader;
    
    static async Task Main(string[] args)
    {
        var host = "127.0.0.1";
        var port = 8080;
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "graphql_db");
        
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║         SharpGraph - GraphQL Database Server        ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"📁 Database path: {dbPath}");
        Console.WriteLine($"🌐 Server starting at http://{host}:{port}");
        Console.WriteLine();
        
        // Initialize executor and schema loader
        var executor = new GraphQLExecutor(dbPath);
        _schemaLoader = new SchemaLoader(dbPath, executor);
        
        Console.WriteLine("✅ Schema loader initialized");
        Console.WriteLine();
        
        // Auto-load existing schema and data on startup
        var schemaPath = Path.Combine(dbPath, "schema.graphql");
        if (File.Exists(schemaPath))
        {
            Console.WriteLine("📂 Loading existing schema from disk...");
            try
            {
                _schemaLoader.LoadSchemaFromFile(schemaPath);
                Console.WriteLine("✅ Schema loaded from disk");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load schema: {ex.Message}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine("ℹ️  No existing schema found. Use POST /schema/load to load a schema.");
            Console.WriteLine();
        }
        Console.WriteLine("Available endpoints:");
        Console.WriteLine("  POST   /graphql              - Execute GraphQL queries");
        Console.WriteLine("  GET    /graphql?query=       - Execute GraphQL queries (GET)");
        Console.WriteLine("  GET    /graphql?sdl          - Get GraphQL SDL");
        Console.WriteLine();
        Console.WriteLine("  POST   /schema/load          - Load schema from SDL");
        Console.WriteLine("  POST   /schema/data          - Load data into tables");
        Console.WriteLine("  GET    /schema               - Get current schema");
        Console.WriteLine("  GET    /schema/tables        - List all tables");
        Console.WriteLine();
        Console.WriteLine("Server ready! Press Ctrl+C to stop.");
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine();
        
        // Start HTTP listener
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{host}:{port}/");
        listener.Start();
        
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n\n🛑 Shutting down...");
        };
        
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var contextTask = listener.GetContextAsync();
                var context = await contextTask.WaitAsync(cts.Token);
                
                _ = Task.Run(() => HandleRequest(context, executor, _schemaLoader!, dbPath), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("✅ Server stopped cleanly");
        }
    }
    
    static async Task HandleRequest(HttpListenerContext context, GraphQLExecutor executor, SchemaLoader schemaLoader, string dbPath)
    {
        var request = context.Request;
        var response = context.Response;
        
        // Add CORS headers
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        
        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 200;
            response.Close();
            return;
        }
        
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var path = request.Url?.AbsolutePath ?? "/";
        
        Console.WriteLine($"[{timestamp}] {request.HttpMethod} {path}");
        
        try
        {
            // Schema management endpoints
            if (path.StartsWith("/schema"))
            {
                await HandleSchemaRequest(context, schemaLoader, timestamp);
                return;
            }
            
            if (path == "/graphql" || path == "/")
            {
                if (request.HttpMethod == "GET")
                {
                    // Check for SDL request
                    var queryString = request.Url?.Query;
                    if (queryString?.Contains("sdl") == true)
                    {
                        var schemaPath = Path.Combine(dbPath, "schema.graphql");
                        var schema = File.Exists(schemaPath) ? File.ReadAllText(schemaPath) : "No schema loaded";
                        await SendTextResponse(response, schema, "text/plain");
                        Console.WriteLine($"[{timestamp}]   → 200 OK (SDL schema)");
                        return;
                    }
                    
                    // GET query
                    var query = request.QueryString["query"];
                    if (!string.IsNullOrEmpty(query))
                    {
                        var variablesJson = request.QueryString["variables"];
                        JsonElement? variables = null;
                        
                        if (!string.IsNullOrEmpty(variablesJson))
                        {
                            variables = JsonDocument.Parse(variablesJson).RootElement;
                        }
                        
                        var result = executor.Execute(query, variables);
                        await SendJsonResponse(response, result);
                        Console.WriteLine($"[{timestamp}]   → 200 OK (query executed)");
                        return;
                    }
                    
                    // No query, send help
                    await SendTextResponse(response, "SharpGraph GraphQL Server\n\nPOST /graphql with JSON body: { \"query\": \"...\", \"variables\": {...} }\nGET /graphql?query=...\nGET /graphql?sdl", "text/plain");
                    return;
                }
                
                if (request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync();
                    
                    Console.WriteLine($"[{timestamp}]   Body: {body.Substring(0, Math.Min(100, body.Length))}...");
                    
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        await SendJsonResponse(response, JsonDocument.Parse("{\"errors\":[{\"message\":\"Empty request body\"}]}"), 400);
                        Console.WriteLine($"[{timestamp}]   → 400 Bad Request (empty body)");
                        return;
                    }
                    
                    JsonDocument requestData;
                    try
                    {
                        requestData = JsonDocument.Parse(body);
                    }
                    catch (JsonException ex)
                    {
                        await SendJsonResponse(response, JsonDocument.Parse($"{{\"errors\":[{{\"message\":\"Invalid JSON: {ex.Message}\"}}]}}"), 400);
                        Console.WriteLine($"[{timestamp}]   → 400 Bad Request (invalid JSON)");
                        return;
                    }
                    
                    string? query = null;
                    if (requestData.RootElement.TryGetProperty("query", out var queryProp))
                    {
                        query = queryProp.GetString();
                    }
                    
                    JsonElement? variables = null;
                    if (requestData.RootElement.TryGetProperty("variables", out var varsElement))
                    {
                        variables = varsElement;
                    }
                    
                    if (string.IsNullOrEmpty(query))
                    {
                        await SendJsonResponse(response, JsonDocument.Parse("{\"errors\":[{\"message\":\"No query provided\"}]}"), 400);
                        Console.WriteLine($"[{timestamp}]   → 400 Bad Request (no query)");
                        return;
                    }
                    
                    var result = executor.Execute(query, variables);
                    await SendJsonResponse(response, result);
                    
                    var preview = result.RootElement.ToString();
                    Console.WriteLine($"[{timestamp}]   → 200 OK ({preview.Substring(0, Math.Min(50, preview.Length))}...)");
                    return;
                }
            }
            
            response.StatusCode = 404;
            await SendTextResponse(response, "Not Found", "text/plain", 404);
            Console.WriteLine($"[{timestamp}]   → 404 Not Found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{timestamp}]   ❌ Error: {ex.Message}");
            
            var error = new { errors = new[] { new { message = ex.Message } } };
            var errorJson = JsonSerializer.Serialize(error);
            await SendTextResponse(response, errorJson, "application/json", 500);
        }
    }
    
    static async Task HandleSchemaRequest(HttpListenerContext context, SchemaLoader schemaLoader, string timestamp)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? "/";
        
        try
        {
            // GET /schema - Get all schemas
            if (path == "/schema" && request.HttpMethod == "GET")
            {
                var tables = schemaLoader.GetTables();
                var schemasInfo = tables.Select(t => new
                {
                    tableName = t.Key,
                    recordCount = t.Value.SelectAll().Count()
                }).ToList();
                
                var json = JsonSerializer.Serialize(new { tables = schemasInfo }, new JsonSerializerOptions { WriteIndented = true });
                await SendTextResponse(response, json, "application/json");
                Console.WriteLine($"[{timestamp}]   → 200 OK (listed {tables.Count} tables)");
                return;
            }
            
            // GET /schema/tables - List all tables with details
            if (path == "/schema/tables" && request.HttpMethod == "GET")
            {
                var tables = schemaLoader.GetTables();
                var tablesInfo = tables.Select(t => new
                {
                    name = t.Key,
                    recordCount = t.Value.SelectAll().Count(),
                    filePath = t.Key + ".tbl"
                }).ToList();
                
                var json = JsonSerializer.Serialize(new { tables = tablesInfo }, new JsonSerializerOptions { WriteIndented = true });
                await SendTextResponse(response, json, "application/json");
                Console.WriteLine($"[{timestamp}]   → 200 OK (table details)");
                return;
            }
            
            // POST /schema/load - Load schema from SDL
            if (path == "/schema/load" && request.HttpMethod == "POST")
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(body))
                {
                    await SendTextResponse(response, "{\"error\":\"Empty schema\"}", "application/json", 400);
                    Console.WriteLine($"[{timestamp}]   → 400 Bad Request (empty schema)");
                    return;
                }
                
                try
                {
                    // Check if body is JSON with "schema" property or raw SDL
                    string schemaContent;
                    if (body.TrimStart().StartsWith("{"))
                    {
                        var jsonDoc = JsonDocument.Parse(body);
                        schemaContent = jsonDoc.RootElement.GetProperty("schema").GetString() ?? "";
                    }
                    else
                    {
                        schemaContent = body;
                    }
                    
                    schemaLoader.LoadSchema(schemaContent);
                    
                    var tables = schemaLoader.GetTables();
                    var result = new
                    {
                        success = true,
                        message = $"Schema loaded successfully. Created {tables.Count} tables.",
                        tables = tables.Keys.ToList()
                    };
                    
                    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                    await SendTextResponse(response, json, "application/json");
                    Console.WriteLine($"[{timestamp}]   → 200 OK (schema loaded, {tables.Count} tables)");
                    return;
                }
                catch (Exception ex)
                {
                    var error = new { success = false, error = ex.Message };
                    var json = JsonSerializer.Serialize(error);
                    await SendTextResponse(response, json, "application/json", 400);
                    Console.WriteLine($"[{timestamp}]   → 400 Bad Request ({ex.Message})");
                    return;
                }
            }
            
            // POST /schema/data - Load data into tables
            if (path == "/schema/data" && request.HttpMethod == "POST")
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(body))
                {
                    await SendTextResponse(response, "{\"error\":\"Empty data\"}", "application/json", 400);
                    Console.WriteLine($"[{timestamp}]   → 400 Bad Request (empty data)");
                    return;
                }
                
                try
                {
                    schemaLoader.LoadData(body);
                    
                    var tables = schemaLoader.GetTables();
                    var recordCounts = tables.ToDictionary(
                        t => t.Key,
                        t => t.Value.SelectAll().Count()
                    );
                    
                    var result = new
                    {
                        success = true,
                        message = "Data loaded successfully",
                        recordCounts
                    };
                    
                    var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                    await SendTextResponse(response, json, "application/json");
                    Console.WriteLine($"[{timestamp}]   → 200 OK (data loaded)");
                    return;
                }
                catch (Exception ex)
                {
                    var error = new { success = false, error = ex.Message };
                    var json = JsonSerializer.Serialize(error);
                    await SendTextResponse(response, json, "application/json", 400);
                    Console.WriteLine($"[{timestamp}]   → 400 Bad Request ({ex.Message})");
                    return;
                }
            }
            
            response.StatusCode = 404;
            await SendTextResponse(response, "{\"error\":\"Endpoint not found\"}", "application/json", 404);
            Console.WriteLine($"[{timestamp}]   → 404 Not Found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{timestamp}]   ❌ Error: {ex.Message}");
            var error = new { success = false, error = ex.Message };
            var json = JsonSerializer.Serialize(error);
            await SendTextResponse(response, json, "application/json", 500);
        }
    }
    
    static async Task SendJsonResponse(HttpListenerContext context, JsonDocument document, int statusCode = 200)
    {
        await SendJsonResponse(context.Response, document, statusCode);
    }
    
    static async Task SendJsonResponse(HttpListenerResponse response, JsonDocument document, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        
        var json = document.RootElement.ToString();
        var buffer = Encoding.UTF8.GetBytes(json);
        
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
    
    static async Task SendTextResponse(HttpListenerResponse response, string text, string contentType, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentType = contentType;
        
        var buffer = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
    
    static GraphQLScalarType MapGraphQLTypeToScalarType(TypeNode typeNode)
    {
        // Unwrap NonNull and List wrappers
        var unwrapped = typeNode;
        while (unwrapped is NonNullType nonNull)
            unwrapped = nonNull.Type;
        
        if (unwrapped is ListType list)
            unwrapped = list.Type;
        
        if (unwrapped is NamedType named)
        {
            return named.Name switch
            {
                "ID" => GraphQLScalarType.ID,
                "String" => GraphQLScalarType.String,
                "Int" => GraphQLScalarType.Int,
                "Float" => GraphQLScalarType.Float,
                "Boolean" => GraphQLScalarType.Boolean,
                _ => GraphQLScalarType.String
            };
        }
        
        return GraphQLScalarType.String;
    }
    
    static bool IsNonNull(TypeNode typeNode)
    {
        return typeNode is NonNullType;
    }
    
    static bool IsList(TypeNode typeNode)
    {
        var unwrapped = typeNode;
        while (unwrapped is NonNullType nonNull)
            unwrapped = nonNull.Type;
        
        return unwrapped is ListType;
    }
}
