using Cis.Api.Security;
using Cis.Contracts;
using Serilog.Context;

namespace Cis.Api.Middleware;

internal sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(StandardHeaders.CorrelationId, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : context.TraceIdentifier;

        context.Request.Headers[StandardHeaders.CorrelationId] = correlationId;
        context.Response.Headers[StandardHeaders.CorrelationId] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
