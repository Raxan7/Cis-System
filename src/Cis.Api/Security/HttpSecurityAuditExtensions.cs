using Cis.Application.Common.Interfaces;
using Cis.Contracts;
using Cis.Domain.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Api.Security;

internal static class HttpSecurityAuditExtensions
{
    public static async Task WriteSecurityAuditAsync(
        this HttpContext httpContext,
        string action,
        AuditEventType eventType,
        string summary)
    {
        var auditWriter = httpContext.RequestServices.GetService<IAuditLogWriter>();
        if (auditWriter is null)
        {
            return;
        }

        var identity = httpContext.User?.Identity;
        var actorId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User?.FindFirst("sub")?.Value;
        var actorName = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? identity?.Name;
        var actorRole = httpContext.User?.FindAll(System.Security.Claims.ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        await auditWriter.WriteAsync(new AuditLogEntry(
            Module: "Security",
            Action: action,
            EntityName: "HttpEndpoint",
            EntityId: httpContext.Request.Path,
            EventType: eventType,
            ActorId: actorId,
            ActorDisplayName: actorName,
            ActorRole: actorRole is null ? null : string.Join(",", actorRole),
            CorrelationId: httpContext.Request.Headers[StandardHeaders.CorrelationId].ToString(),
            IpAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: httpContext.Request.Headers.UserAgent.ToString(),
            Summary: summary,
            Reason: summary,
            AfterJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                Method = httpContext.Request.Method,
                Path = httpContext.Request.Path.Value,
                QueryString = httpContext.Request.QueryString.Value,
                IsAuthenticated = identity?.IsAuthenticated == true
            })),
            httpContext.RequestAborted);
    }
}
