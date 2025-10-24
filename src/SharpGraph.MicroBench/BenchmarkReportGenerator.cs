namespace SharpGraph.MicroBench;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates benchmark reports in various formats
/// </summary>
public class BenchmarkReportGenerator
{
    /// <summary>
    /// Generate JSON report
    /// </summary>
    public static string GenerateJson(BenchmarkSuite suite)
    {
        var lines = new List<string>();
        lines.Add("{");
        lines.Add($"  \"executionTime\": \"{suite.TotalExecutionTime.TotalSeconds:F2}s\",");
        lines.Add($"  \"benchmarkCount\": {suite.Results.Count},");
        lines.Add("  \"results\": [");
        
        for (int i = 0; i < suite.Results.Count; i++)
        {
            var result = suite.Results[i];
            lines.Add("    {");
            lines.Add($"      \"name\": \"{EscapeJson(result.Name)}\",");
            lines.Add($"      \"iterations\": {result.Iterations},");
            lines.Add($"      \"successfulOperations\": {result.SuccessfulOperations},");
            lines.Add($"      \"failedOperations\": {result.FailedOperations},");
            lines.Add($"      \"successRate\": {result.GetSuccessRatePercent():F2},");
            lines.Add($"      \"totalMs\": {result.TotalMs:F4},");
            lines.Add($"      \"averageMs\": {result.GetAverageMs():F4},");
            lines.Add($"      \"minMs\": {result.GetMinMs():F4},");
            lines.Add($"      \"maxMs\": {result.GetMaxMs():F4},");
            lines.Add($"      \"medianMs\": {result.GetMedianMs():F4},");
            lines.Add($"      \"p95Ms\": {result.GetPercentileMs(95):F4},");
            lines.Add($"      \"p99Ms\": {result.GetPercentileMs(99):F4},");
            lines.Add($"      \"opsPerSec\": {result.GetOpsPerSecond():F2}");
            lines.Add(i < suite.Results.Count - 1 ? "    }," : "    }");
        }
        
        lines.Add("  ]");
        lines.Add("}");
        
        return string.Join("\n", lines);
    }
    
    /// <summary>
    /// Generate CSV report
    /// </summary>
    public static string GenerateCsv(BenchmarkSuite suite)
    {
        var lines = new List<string>();
        lines.Add("Name,Iterations,Successful,Failed,SuccessRate%,TotalMs,AvgMs,MinMs,MaxMs,MedianMs,P95Ms,P99Ms,OpsPerSec");
        
        foreach (var result in suite.Results)
        {
            lines.Add($"{EscapeCsv(result.Name)}," +
                $"{result.Iterations}," +
                $"{result.SuccessfulOperations}," +
                $"{result.FailedOperations}," +
                $"{result.GetSuccessRatePercent():F2}," +
                $"{result.TotalMs:F4}," +
                $"{result.GetAverageMs():F4}," +
                $"{result.GetMinMs():F4}," +
                $"{result.GetMaxMs():F4}," +
                $"{result.GetMedianMs():F4}," +
                $"{result.GetPercentileMs(95):F4}," +
                $"{result.GetPercentileMs(99):F4}," +
                $"{result.GetOpsPerSecond():F2}");
        }
        
        return string.Join("\n", lines);
    }
    
    /// <summary>
    /// Generate HTML report with charts
    /// </summary>
    public static string GenerateHtml(BenchmarkSuite suite)
    {
        var html = new System.Text.StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <title>SharpGraph Benchmark Report</title>");
        html.AppendLine("  <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
        html.AppendLine("    .container { max-width: 1200px; margin: 0 auto; }");
        html.AppendLine("    h1 { color: #333; border-bottom: 2px solid #0066cc; padding-bottom: 10px; }");
        html.AppendLine("    h2 { color: #0066cc; margin-top: 30px; }");
        html.AppendLine("    .summary { background: white; padding: 15px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); margin: 10px 0; }");
        html.AppendLine("    .metric { display: inline-block; margin-right: 30px; }");
        html.AppendLine("    .metric-label { font-weight: bold; color: #666; }");
        html.AppendLine("    .metric-value { font-size: 1.4em; color: #0066cc; }");
        html.AppendLine("    table { border-collapse: collapse; width: 100%; background: white; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        html.AppendLine("    th, td { border: 1px solid #ddd; padding: 12px; text-align: right; }");
        html.AppendLine("    th { background: #0066cc; color: white; font-weight: bold; }");
        html.AppendLine("    td:first-child, th:first-child { text-align: left; }");
        html.AppendLine("    tr:nth-child(even) { background: #f9f9f9; }");
        html.AppendLine("    .chart-container { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        html.AppendLine("    canvas { max-height: 300px; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class=\"container\">");
        html.AppendLine($"<h1>SharpGraph Benchmark Report</h1>");
        html.AppendLine($"<div class=\"summary\">");
        html.AppendLine($"  <div class=\"metric\"><div class=\"metric-label\">Benchmarks</div><div class=\"metric-value\">{suite.Results.Count}</div></div>");
        html.AppendLine($"  <div class=\"metric\"><div class=\"metric-label\">Total Time</div><div class=\"metric-value\">{suite.TotalExecutionTime.TotalSeconds:F2}s</div></div>");
        html.AppendLine($"  <div class=\"metric\"><div class=\"metric-label\">Timestamp</div><div class=\"metric-value\">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</div></div>");
        html.AppendLine($"</div>");
        
        html.AppendLine("<h2>Results Summary</h2>");
        html.AppendLine("<table>");
        html.AppendLine("  <tr>");
        html.AppendLine("    <th>Benchmark</th>");
        html.AppendLine("    <th>Operations</th>");
        html.AppendLine("    <th>Success Rate</th>");
        html.AppendLine("    <th>Avg (ms)</th>");
        html.AppendLine("    <th>Min (ms)</th>");
        html.AppendLine("    <th>Max (ms)</th>");
        html.AppendLine("    <th>P95 (ms)</th>");
        html.AppendLine("    <th>P99 (ms)</th>");
        html.AppendLine("    <th>Ops/sec</th>");
        html.AppendLine("  </tr>");
        
        foreach (var result in suite.Results)
        {
            html.AppendLine("  <tr>");
            html.AppendLine($"    <td>{result.Name}</td>");
            html.AppendLine($"    <td>{result.SuccessfulOperations}</td>");
            html.AppendLine($"    <td>{result.GetSuccessRatePercent():F2}%</td>");
            html.AppendLine($"    <td>{result.GetAverageMs():F4}</td>");
            html.AppendLine($"    <td>{result.GetMinMs():F4}</td>");
            html.AppendLine($"    <td>{result.GetMaxMs():F4}</td>");
            html.AppendLine($"    <td>{result.GetPercentileMs(95):F4}</td>");
            html.AppendLine($"    <td>{result.GetPercentileMs(99):F4}</td>");
            html.AppendLine($"    <td>{result.GetOpsPerSecond():F2}</td>");
            html.AppendLine("  </tr>");
        }
        
        html.AppendLine("</table>");
        
        // Add charts
        html.AppendLine("<h2>Performance Charts</h2>");
        html.AppendLine("<div class=\"chart-container\">");
        html.AppendLine("  <h3>Operations per Second</h3>");
        html.AppendLine("  <canvas id=\"opsChart\"></canvas>");
        html.AppendLine("</div>");
        
        html.AppendLine("<div class=\"chart-container\">");
        html.AppendLine("  <h3>Average Operation Time (ms)</h3>");
        html.AppendLine("  <canvas id=\"avgChart\"></canvas>");
        html.AppendLine("</div>");
        
        html.AppendLine("<div class=\"chart-container\">");
        html.AppendLine("  <h3>Latency Distribution (P50/P95/P99)</h3>");
        html.AppendLine("  <canvas id=\"percentileChart\"></canvas>");
        html.AppendLine("</div>");
        
        // Add JavaScript for charts
        html.AppendLine("<script>");
        GenerateChartScripts(html, suite);
        html.AppendLine("</script>");
        
        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        return html.ToString();
    }
    
    private static void GenerateChartScripts(System.Text.StringBuilder html, BenchmarkSuite suite)
    {
        var names = suite.Results.Select(r => r.Name).ToList();
        var opsPerSec = suite.Results.Select(r => r.GetOpsPerSecond()).ToList();
        var avgMs = suite.Results.Select(r => r.GetAverageMs()).ToList();
        var p50Ms = suite.Results.Select(r => r.GetMedianMs()).ToList();
        var p95Ms = suite.Results.Select(r => r.GetPercentileMs(95)).ToList();
        var p99Ms = suite.Results.Select(r => r.GetPercentileMs(99)).ToList();
        
        // Ops chart
        html.AppendLine("  const opsCtx = document.getElementById('opsChart').getContext('2d');");
        html.AppendLine("  new Chart(opsCtx, {");
        html.AppendLine("    type: 'bar',");
        html.AppendLine($"    data: {{");
        html.AppendLine($"      labels: {JsonArray(names)},");
        html.AppendLine($"      datasets: [{{");
        html.AppendLine($"        label: 'Ops/sec',");
        html.AppendLine($"        data: {JsonArray(opsPerSec)},");
        html.AppendLine($"        backgroundColor: 'rgba(0, 102, 204, 0.6)',");
        html.AppendLine($"        borderColor: 'rgba(0, 102, 204, 1)',");
        html.AppendLine($"        borderWidth: 1");
        html.AppendLine($"      }}]");
        html.AppendLine($"    }},");
        html.AppendLine($"    options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}");
        html.AppendLine("  });");
        
        // Avg chart
        html.AppendLine("  const avgCtx = document.getElementById('avgChart').getContext('2d');");
        html.AppendLine("  new Chart(avgCtx, {");
        html.AppendLine("    type: 'bar',");
        html.AppendLine($"    data: {{");
        html.AppendLine($"      labels: {JsonArray(names)},");
        html.AppendLine($"      datasets: [{{");
        html.AppendLine($"        label: 'Avg (ms)',");
        html.AppendLine($"        data: {JsonArray(avgMs)},");
        html.AppendLine($"        backgroundColor: 'rgba(102, 153, 0, 0.6)',");
        html.AppendLine($"        borderColor: 'rgba(102, 153, 0, 1)',");
        html.AppendLine($"        borderWidth: 1");
        html.AppendLine($"      }}]");
        html.AppendLine($"    }},");
        html.AppendLine($"    options: {{ responsive: true, plugins: {{ legend: {{ display: false }} }} }}");
        html.AppendLine("  });");
        
        // Percentile chart
        html.AppendLine("  const percCtx = document.getElementById('percentileChart').getContext('2d');");
        html.AppendLine("  new Chart(percCtx, {");
        html.AppendLine("    type: 'line',");
        html.AppendLine($"    data: {{");
        html.AppendLine($"      labels: {JsonArray(names)},");
        html.AppendLine($"      datasets: [");
        html.AppendLine($"        {{ label: 'P50', data: {JsonArray(p50Ms)}, borderColor: 'rgba(0, 102, 204, 1)', fill: false }},");
        html.AppendLine($"        {{ label: 'P95', data: {JsonArray(p95Ms)}, borderColor: 'rgba(255, 165, 0, 1)', fill: false }},");
        html.AppendLine($"        {{ label: 'P99', data: {JsonArray(p99Ms)}, borderColor: 'rgba(255, 0, 0, 1)', fill: false }}");
        html.AppendLine($"      ]");
        html.AppendLine($"    }},");
        html.AppendLine($"    options: {{ responsive: true }}");
        html.AppendLine("  });");
    }
    
    private static string JsonArray<T>(List<T> items)
    {
        return "[" + string.Join(", ", items.Select(i => 
            i is string s ? $"\"{EscapeJson(s)}\"" : Convert.ToString(i))) + "]";
    }
    
    private static string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
    
    private static string EscapeCsv(string text)
    {
        if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        return text;
    }
    
    /// <summary>
    /// Generate Markdown report
    /// </summary>
    public static string GenerateMarkdown(BenchmarkSuite suite)
    {
        var lines = new List<string>();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Header
        lines.Add("# Benchmark Results");
        lines.Add("");
        lines.Add($"**Generated:** {timestamp}");
        lines.Add($"**Total Execution Time:** {suite.TotalExecutionTime.TotalSeconds:F2} seconds");
        lines.Add($"**Total Benchmarks:** {suite.Results.Count}");
        lines.Add("");
        
        // Summary table
        lines.Add("## Summary");
        lines.Add("");
        lines.Add("| Benchmark | Category | Ops/sec | Avg (ms) | P95 (ms) | P99 (ms) | Success Rate |");
        lines.Add("|-----------|----------|---------|----------|----------|----------|--------------|");
        
        foreach (var result in suite.Results)
        {
            lines.Add($"| {result.Name} | {result.Category} | {result.GetOpsPerSecond():F2} | {result.GetAverageMs():F4} | {result.GetPercentileMs(95):F4} | {result.GetPercentileMs(99):F4} | {result.GetSuccessRatePercent():F2}% |");
        }
        
        lines.Add("");
        
        // Detailed results
        lines.Add("## Detailed Results");
        lines.Add("");
        
        foreach (var result in suite.Results)
        {
            lines.Add($"### {result.Name}");
            lines.Add("");
            lines.Add($"**Description:** {result.Description}");
            lines.Add($"**Category:** {result.Category}");
            lines.Add("");
            
            lines.Add("#### Execution Statistics");
            lines.Add("");
            lines.Add($"- **Total Iterations:** {result.Iterations}");
            lines.Add($"- **Successful Operations:** {result.SuccessfulOperations}");
            lines.Add($"- **Failed Operations:** {result.FailedOperations}");
            lines.Add($"- **Success Rate:** {result.GetSuccessRatePercent():F2}%");
            lines.Add($"- **Total Time:** {result.TotalMs:F2} ms");
            lines.Add("");
            
            lines.Add("#### Latency Analysis");
            lines.Add("");
            lines.Add($"- **Min:** {result.GetMinMs():F4} ms");
            lines.Add($"- **Average:** {result.GetAverageMs():F4} ms");
            lines.Add($"- **Median:** {result.GetMedianMs():F4} ms");
            lines.Add($"- **P95:** {result.GetPercentileMs(95):F4} ms");
            lines.Add($"- **P99:** {result.GetPercentileMs(99):F4} ms");
            lines.Add($"- **Max:** {result.GetMaxMs():F4} ms");
            lines.Add("");
            
            lines.Add("#### Throughput");
            lines.Add("");
            lines.Add($"- **Operations/Second:** {result.GetOpsPerSecond():F2}");
            lines.Add("");
            
            if (result.Errors.Count > 0)
            {
                lines.Add("#### Errors");
                lines.Add("");
                foreach (var error in result.Errors.Take(5))
                {
                    lines.Add($"- {error}");
                }
                if (result.Errors.Count > 5)
                {
                    lines.Add($"- ... and {result.Errors.Count - 5} more errors");
                }
                lines.Add("");
            }
        }
        
        // Summary statistics
        lines.Add("## Summary Statistics");
        lines.Add("");
        
        var avgThroughput = suite.Results.Average(r => r.GetOpsPerSecond());
        var maxP99 = suite.Results.Max(r => r.GetPercentileMs(99));
        var successRate = suite.Results.Average(r => r.GetSuccessRatePercent());
        
        lines.Add($"- **Average Throughput:** {avgThroughput:F2} ops/sec");
        lines.Add($"- **Max P99 Latency:** {maxP99:F4} ms");
        lines.Add($"- **Average Success Rate:** {successRate:F2}%");
        lines.Add("");
        
        return string.Join("\n", lines);
    }
}
