namespace Dealio.API.Infrastructure;

using System.Collections.Concurrent;
using System.Diagnostics;

public sealed class ApiRequestMetrics
{
    private readonly ConcurrentQueue<(long Timestamp, int StatusCode, double DurationMs)> _samples = new();

    public void Record(int statusCode, double durationMs)
    {
        var now = Stopwatch.GetTimestamp();
        _samples.Enqueue((now, statusCode, durationMs));
        while (_samples.Count > 10_000 || (_samples.TryPeek(out var oldest)
            && Stopwatch.GetElapsedTime(oldest.Timestamp, now) > TimeSpan.FromMinutes(15)))
            _samples.TryDequeue(out _);
    }

    public object Snapshot()
    {
        var now = Stopwatch.GetTimestamp();
        var rows = _samples.Where(row => Stopwatch.GetElapsedTime(row.Timestamp, now) <= TimeSpan.FromMinutes(15))
            .Select(row => row.DurationMs).Order().ToArray();
        return new
        {
            windowMinutes = 15,
            requestCount = rows.Length,
            averageLatencyMs = rows.Length == 0 ? 0 : Math.Round(rows.Average(), 1),
            p95LatencyMs = rows.Length == 0 ? 0 : Math.Round(rows[(int)Math.Ceiling(rows.Length * .95) - 1], 1)
        };
    }
}

public sealed class ApiRequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApiRequestMetrics metrics)
    {
        var started = Stopwatch.GetTimestamp();
        try { await next(context); }
        finally { metrics.Record(context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds); }
    }
}
