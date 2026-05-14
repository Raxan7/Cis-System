using Cis.Application.Common.Interfaces;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Audit;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuditLogWriterTests
{
    private readonly CisApiFactory _factory;

    public AuditLogWriterTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WriteAsync_PersistsAuditLog()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        var writer = scope.ServiceProvider.GetRequiredService<IAuditLogWriter>();
        var entityId = $"scheme-{Guid.NewGuid():N}";

        await writer.WriteAsync(new AuditLogEntry(
            "Schemes",
            "Create",
            "Scheme",
            entityId,
            ActorId: "maker-001",
            ActorDisplayName: "Fund Maker",
            Summary: "Created scheme draft.",
            IdempotencyKey: "test-key-001"));

        var auditLog = await dbContext.AuditLogs.SingleAsync(candidate => candidate.EntityId == entityId);

        auditLog.Module.Should().Be("Schemes");
        auditLog.Action.Should().Be("Create");
        auditLog.EntityName.Should().Be("Scheme");
        auditLog.EntityId.Should().Be(entityId);
        auditLog.ActorId.Should().Be("maker-001");
        auditLog.IdempotencyKey.Should().Be("test-key-001");
        auditLog.OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }
}
