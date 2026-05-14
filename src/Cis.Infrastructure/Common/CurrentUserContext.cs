using System.Diagnostics;
using System.Security.Claims;
using Cis.Application.Common.Interfaces;
using Cis.Contracts;
using Microsoft.AspNetCore.Http;

namespace Cis.Infrastructure.Common;

internal sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => ClaimValue(ClaimTypes.NameIdentifier)
        ?? ClaimValue("sub");

    public string? DisplayName => ClaimValue(ClaimTypes.Name)
        ?? ClaimValue("name")
        ?? ClaimValue("preferred_username");

    public IReadOnlyCollection<string> Roles => _httpContextAccessor.HttpContext?.User
        .FindAll(ClaimTypes.Role)
        .Select(claim => claim.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray()
        ?? [];

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? CorrelationId => Header(StandardHeaders.CorrelationId)
        ?? Activity.Current?.TraceId.ToString()
        ?? _httpContextAccessor.HttpContext?.TraceIdentifier;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => Header("User-Agent");

    private string? ClaimValue(string type)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(type)?.Value;
    }

    private string? Header(string name)
    {
        if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(name, out var values) == true)
        {
            return values.ToString();
        }

        return null;
    }
}
