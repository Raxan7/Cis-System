using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Contracts;
using Cis.Contracts.CustodyReconciliation;
using Cis.Contracts.Identity;
using Cis.Application.Common.Security;
using Cis.Domain.Audit;
using Cis.Domain.CustodyReconciliation;
using Cis.Domain.Identity;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.CustodyReconciliation;

[Collection(IntegrationTestCollection.Name)]
public sealed class CustodyReconciliationModuleTests
{
    private readonly CisApiFactory _factory;

    public CustodyReconciliationModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedCustodyEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/custody/reconciliation-breaks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedCustodyEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("custody-portal"), "Custody Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync("/api/custody/reconciliation-breaks");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CustodianAndIdempotentStatementImports_CreateAuditAndRejectKeyReuseWithDifferentPayload()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var schemeId = await SeedSchemeAsync("CUST-IDEM");
        var custodian = await CreateCustodianAsync(client, schemeId, "IDCUST");

        var request = HoldingsImportRequest(custodian.Id, schemeId, "statement-idem.csv", 100m);
        var first = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/holdings/import", request, "idem-holdings-1");
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstImport = await ReadResponseAsync<CustodianStatementImportDto>(first);

        var repeat = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/holdings/import", request, "idem-holdings-1");
        repeat.StatusCode.Should().Be(HttpStatusCode.Created);
        var repeatedImport = await ReadResponseAsync<CustodianStatementImportDto>(repeat);
        repeatedImport.Id.Should().Be(firstImport.Id);

        var differentPayload = HoldingsImportRequest(custodian.Id, schemeId, "statement-idem.csv", 125m);
        var conflict = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/holdings/import", differentPayload, "idem-holdings-1");
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.CustodianStatementImports.CountAsync(import => import.IdempotencyKey == "idem-holdings-1")).Should().Be(1);
        var actions = await dbContext.AuditLogs.Where(log => log.Module == "CustodyReconciliation").Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["CustodianCreated", "CustodianHoldingsImported"]);
    }

    [Fact]
    public async Task ReconciliationRun_GeneratesHoldingCashSettlementBreaksAgingAndPreservesResolutionHistory()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var schemeId = await SeedSchemeAsync("CUST-BREAKS");
        var custodian = await CreateCustodianAsync(client, schemeId, "BRCUST");

        var holdingsResponse = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/holdings/import", HoldingsImportRequest(custodian.Id, schemeId, "holdings-breaks.csv", 100m, includeUnsettled: true), $"holdings-{Guid.NewGuid():N}");
        holdingsResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var holdingsImport = await ReadResponseAsync<CustodianStatementImportDto>(holdingsResponse);
        var cashResponse = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/cash/import", CashImportRequest(custodian.Id, schemeId, "cash-breaks.csv", 1_000m, includeUnsettled: true), $"cash-{Guid.NewGuid():N}");
        cashResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var cashImport = await ReadResponseAsync<CustodianStatementImportDto>(cashResponse);

        var runRequest = new CreateCustodyReconciliationRunRequest(
            custodian.Id,
            holdingsImport.Id,
            cashImport.Id,
            new DateOnly(2026, 9, 30),
            [
                new InternalHoldingSnapshotRequest(schemeId, null, "TBILL-91", 95m, 1_000m, "KES"),
                new InternalHoldingSnapshotRequest(schemeId, null, "INTERNAL-ONLY", 15m, 150m, "KES")
            ],
            [
                new InternalCashSnapshotRequest(schemeId, "CASH-001", "KES", 800m),
                new InternalCashSnapshotRequest(schemeId, "CASH-INTERNAL", "KES", 50m)
            ]);

        var runResponse = await client.PostAsJsonAsync("/api/custody/reconciliation-runs", runRequest);
        var body = await runResponse.Content.ReadAsStringAsync();
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var run = await ReadResponseAsync<CustodyReconciliationRunDto>(runResponse);
        run.Breaks.Should().Contain(breakItem => breakItem.BreakCategory == "Holdings" && breakItem.BreakType == ReconciliationBreakType.QuantityMismatch.ToString());
        run.Breaks.Should().Contain(breakItem => breakItem.BreakCategory == "Holdings" && breakItem.BreakType == ReconciliationBreakType.MissingCustodian.ToString());
        run.Breaks.Should().Contain(breakItem => breakItem.BreakType == ReconciliationBreakType.SettlementUnconfirmed.ToString());
        run.Breaks.Should().Contain(breakItem => breakItem.BreakCategory == "Cash" && breakItem.BreakType == ReconciliationBreakType.CashAmountMismatch.ToString());
        run.Breaks.Should().OnlyContain(breakItem => breakItem.LatestAgeDays >= 0);

        var targetBreak = run.Breaks.First();
        var assignResponse = await client.PostAsJsonAsync($"/api/custody/reconciliation-breaks/{targetBreak.Id}/assign", new AssignCustodyBreakRequest("custody-owner", "Investigate custodian mismatch."));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var assigned = await ReadResponseAsync<CustodyReconciliationBreakDto>(assignResponse);
        assigned.Status.Should().Be(ReconciliationBreakStatus.Assigned.ToString());
        assigned.OwnerUserId.Should().Be("custody-owner");
        assigned.LatestActionNote.Should().Be("Investigate custodian mismatch.");

        var resolveResponse = await client.PostAsJsonAsync($"/api/custody/reconciliation-breaks/{targetBreak.Id}/resolve", new ResolveCustodyBreakRequest("evidence/custody-break-resolution.pdf", "Custodian statement corrected and evidence attached."));
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolved = await ReadResponseAsync<CustodyReconciliationBreakDto>(resolveResponse);
        resolved.Status.Should().Be(ReconciliationBreakStatus.Resolved.ToString());
        resolved.ResolutionEvidenceReference.Should().Be("evidence/custody-break-resolution.pdf");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.BreakAgings.CountAsync(age => age.HoldingBreakId == targetBreak.Id || age.CashBreakId == targetBreak.Id)).Should().BeGreaterThanOrEqualTo(2);
        (await dbContext.BreakActionNotes.CountAsync(note => note.HoldingBreakId == targetBreak.Id || note.CashBreakId == targetBreak.Id)).Should().BeGreaterThanOrEqualTo(2);
        (await dbContext.HoldingsReconciliationBreaks.IgnoreQueryFilters().AnyAsync(item => item.Id == targetBreak.Id)
            || await dbContext.CashReconciliationBreaks.IgnoreQueryFilters().AnyAsync(item => item.Id == targetBreak.Id)).Should().BeTrue();
        var actions = await dbContext.AuditLogs.Where(log => log.Module == "CustodyReconciliation").Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["CustodyReconciliationRunCreated", "CustodyBreakGenerated", "CustodyBreakAssigned", "CustodyBreakResolved"]);
    }

    [Fact]
    public async Task SafekeepingConfirmation_CreatesReportSourceRecordFromReconciledImports()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var schemeId = await SeedSchemeAsync("CUST-SAFE");
        var custodian = await CreateCustodianAsync(client, schemeId, "SFCUST");
        var holdingsResponse = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/holdings/import", HoldingsImportRequest(custodian.Id, schemeId, "holdings-safe.csv", 200m), $"holdings-safe-{Guid.NewGuid():N}");
        var holdingsImport = await ReadResponseAsync<CustodianStatementImportDto>(holdingsResponse);
        var cashResponse = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/cash/import", CashImportRequest(custodian.Id, schemeId, "cash-safe.csv", 2_000m), $"cash-safe-{Guid.NewGuid():N}");
        var cashImport = await ReadResponseAsync<CustodianStatementImportDto>(cashResponse);
        var runResponse = await client.PostAsJsonAsync("/api/custody/reconciliation-runs", new CreateCustodyReconciliationRunRequest(
            custodian.Id,
            holdingsImport.Id,
            cashImport.Id,
            new DateOnly(2026, 9, 30),
            [new InternalHoldingSnapshotRequest(schemeId, null, "TBILL-91", 100m, 200m, "KES")],
            [new InternalCashSnapshotRequest(schemeId, "CASH-001", "KES", 2_000m)]));
        var run = await ReadResponseAsync<CustodyReconciliationRunDto>(runResponse);

        var confirmationResponse = await client.PostAsJsonAsync("/api/custody/safekeeping-confirmations", new CreateSafekeepingConfirmationRequest(
            custodian.Id,
            schemeId,
            run.Id,
            new DateOnly(2026, 9, 30),
            "SAFEKEEPING-20260930",
            true));
        var body = await confirmationResponse.Content.ReadAsStringAsync();
        confirmationResponse.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var confirmation = await ReadResponseAsync<SafekeepingConfirmationDto>(confirmationResponse);
        confirmation.HoldingsCount.Should().Be(1);
        confirmation.CashLineCount.Should().Be(1);
        confirmation.TotalMarketValue.Should().Be(200m);
        confirmation.TotalCashBalance.Should().Be(2_000m);
        confirmation.SettlementConfirmed.Should().BeTrue();
        confirmation.ConfirmationPayloadJson.Should().Contain("TBILL-91");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.SafekeepingConfirmations.AnyAsync(item => item.Id == confirmation.Id)).Should().BeTrue();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "CustodyReconciliation" && log.Action == "SafekeepingConfirmationCreated" && log.EventType == AuditEventType.Created)).Should().BeTrue();
    }

    private async Task<CustodianDto> CreateCustodianAsync(HttpClient client, Guid schemeId, string codePrefix)
    {
        var response = await client.PostAsJsonAsync("/api/custody/custodians", new CreateCustodianRequest(
            $"{codePrefix}-{Guid.NewGuid():N}"[..30],
            $"{codePrefix} Custodian",
            null,
            [new CreateCustodianAccountRequest(schemeId, null, "CASH-001", "Main custody account", "KES")]));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return await ReadResponseAsync<CustodianDto>(response);
    }

    private static ImportCustodianHoldingsRequest HoldingsImportRequest(Guid custodianId, Guid schemeId, string fileName, decimal marketValue, bool includeUnsettled = false)
    {
        var lines = new List<ImportHoldingLineRequest>
        {
            new(null, schemeId, null, null, "TBILL-91", "Treasury Bill 91 Day", 100m, marketValue, "KES", "SET-H-001", true)
        };
        if (includeUnsettled)
        {
            lines.Add(new ImportHoldingLineRequest(null, schemeId, null, null, "UNSETTLED-BOND", "Unsettled Bond", 25m, 250m, "KES", "SET-H-002", false));
        }

        return new ImportCustodianHoldingsRequest(custodianId, null, new DateOnly(2026, 9, 30), fileName, lines);
    }

    private static ImportCustodianCashRequest CashImportRequest(Guid custodianId, Guid schemeId, string fileName, decimal cashBalance, bool includeUnsettled = false)
    {
        var lines = new List<ImportCashLineRequest>
        {
            new(null, schemeId, "CASH-001", "KES", new DateOnly(2026, 9, 30), cashBalance, "SET-C-001", true)
        };
        if (includeUnsettled)
        {
            lines.Add(new ImportCashLineRequest(null, schemeId, "CASH-UNSETTLED", "KES", new DateOnly(2026, 9, 30), 75m, "SET-C-002", false));
        }

        return new ImportCustodianCashRequest(custodianId, null, new DateOnly(2026, 9, 30), fileName, lines);
    }

    private async Task<Guid> SeedSchemeAsync(string codePrefix)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var scheme = Scheme.Create($"{codePrefix}-{Guid.NewGuid():N}"[..30], $"{codePrefix} Scheme", "Unit Trust", "KES", "test-fixture", DateTime.UtcNow);
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return scheme.Id;
    }

    private async Task AuthenticateAsBootstrapAdminAsync(HttpClient client)
    {
        await AuthenticateAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
    }

    private static async Task<AuthTokenResponse> AuthenticateAsync(HttpClient client, string email, string password)
    {
        var token = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return token;
    }

    private static async Task<AuthTokenResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return await ReadResponseAsync<AuthTokenResponse>(response);
    }

    private async Task<(Guid Id, string Email, string Password)> CreateUserWithRoleDirectlyAsync(string email, string displayName, string password, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var role = await dbContext.Roles.SingleAsync(role => role.Name == roleName);
        var now = DateTime.UtcNow;
        var user = User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private static async Task<HttpResponseMessage> PostAsJsonWithIdempotencyAsync<T>(HttpClient client, string uri, T request, string idempotencyKey)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add(StandardHeaders.IdempotencyKey, idempotencyKey);
        return await client.SendAsync(message);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@victoryfs.local";
    }
}
