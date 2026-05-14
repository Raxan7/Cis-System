namespace Cis.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    string? UserId { get; }

    string? DisplayName { get; }

    IReadOnlyCollection<string> Roles { get; }

    bool IsAuthenticated { get; }

    string? CorrelationId { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}
