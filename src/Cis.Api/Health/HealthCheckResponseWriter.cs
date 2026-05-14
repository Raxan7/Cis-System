using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cis.Api.Health;

internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse(
            report.Status.ToString(),
            DateTime.UtcNow,
            ToDecimalMilliseconds(report.TotalDuration),
            report.Entries.Select(entry => new HealthCheckEntryResponse(
                entry.Key,
                entry.Value.Status.ToString(),
                ToDecimalMilliseconds(entry.Value.Duration),
                entry.Value.Description,
                entry.Value.Exception?.Message)));

        var json = JsonSerializer.Serialize(response, JsonSerializerOptions);
        return context.Response.WriteAsync(json);
    }

    private static decimal ToDecimalMilliseconds(TimeSpan duration)
    {
        return decimal.Round((decimal)duration.TotalMilliseconds, 3);
    }

    private sealed record HealthCheckResponse(
        string Status,
        DateTime TimestampUtc,
        decimal DurationMilliseconds,
        IEnumerable<HealthCheckEntryResponse> Entries);

    private sealed record HealthCheckEntryResponse(
        string Name,
        string Status,
        decimal DurationMilliseconds,
        string? Description,
        string? Error);
}
