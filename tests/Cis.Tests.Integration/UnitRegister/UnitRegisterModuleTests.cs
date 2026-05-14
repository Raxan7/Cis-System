using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.UnitRegister;
using Cis.Domain.Common;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.UnitRegister;

[Collection(IntegrationTestCollection.Name)]
public sealed class UnitRegisterModuleTests
{
    private readonly CisApiFactory _factory;

    public UnitRegisterModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedUnitRegisterEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/unit-register/investors/{Guid.NewGuid()}/holdings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedUnitRegisterEndpoint_WithAuthenticatedUserMissingPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var createdUser = await CreateUserAsync(client, UniqueEmail("unit-register-no-permission"), "Unit Register No Permission", "NoPermission123!");
        var token = await LoginAsync(client, createdUser.Email, "NoPermission123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync($"/api/unit-register/investors/{Guid.NewGuid()}/holdings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Adjustment_BeforeApproval_DoesNotCreateLedgerMovement()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync();
        var scheme = await CreateActiveSchemeFixtureAsync();

        var createResponse = await client.PostAsJsonAsync("/api/unit-register/adjustments", AdjustmentRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 100.1234567m, new DateOnly(2026, 5, 1)));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var adjustment = await ReadResponseAsync<UnitAdjustmentDto>(createResponse);
        adjustment.Status.Should().Be("PendingApproval");
        adjustment.Units.Should().Be(100.123457m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var ledgerCount = await dbContext.UnitLedgerEntries.CountAsync(entry => entry.AdjustmentId == adjustment.Id);
        ledgerCount.Should().Be(0);
    }

    [Fact]
    public async Task Adjustment_ApprovedByDifferentUser_PostsAppendOnlyLedgerAndDerivedHoldingWithAuditTrail()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync();
        var scheme = await CreateActiveSchemeFixtureAsync();
        var request = AdjustmentRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 120m, new DateOnly(2026, 5, 2));
        var createResponse = await client.PostAsJsonAsync("/api/unit-register/adjustments", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var adjustment = await ReadResponseAsync<UnitAdjustmentDto>(createResponse);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("unit-register-approver"), "Unit Register Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/unit-register/adjustments/{adjustment.Id}/approve", new ApproveUnitAdjustmentRequest("Approved opening unit adjustment."));
        var approveBody = await approveResponse.Content.ReadAsStringAsync();
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK, approveBody);
        var approved = await ReadResponseAsync<UnitAdjustmentDto>(approveResponse);

        approved.Status.Should().Be("Approved");
        approved.ApprovedByUserId.Should().NotBe(approved.RequestedByUserId);

        var holdingsResponse = await client.GetAsync($"/api/unit-register/investors/{investor.Id}/holdings");
        holdingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var holdings = await ReadResponseAsync<IReadOnlyCollection<UnitHoldingDto>>(holdingsResponse);
        holdings.Should().ContainSingle();
        holdings.Single().Units.Should().Be(120m);
        holdings.Single().RedeemableUnits.Should().Be(120m);

        var registerResponse = await client.GetAsync($"/api/unit-register/schemes/{scheme.SchemeId}/classes/{scheme.ClassId}");
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var register = await ReadResponseAsync<SchemeClassUnitRegisterDto>(registerResponse);
        register.TotalUnits.Should().Be(120m);
        register.HoldingCount.Should().Be(1);
        register.LatestSnapshot.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var ledgerEntries = await dbContext.UnitLedgerEntries.Where(entry => entry.AdjustmentId == adjustment.Id).ToListAsync();
        ledgerEntries.Should().ContainSingle();
        var ledgerSum = ledgerEntries.Sum(entry => entry.BalanceUnits);
        var holding = await dbContext.UnitHoldings.SingleAsync(candidate => candidate.InvestorId == investor.Id && candidate.SchemeId == scheme.SchemeId && candidate.SchemeClassId == scheme.ClassId);
        holding.Units.Should().Be(ledgerSum);

        var actions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.Module == "UnitRegister" && (auditLog.EntityId == adjustment.Id.ToString() || auditLog.EntityId == ledgerEntries.Single().Id.ToString()))
            .Select(auditLog => auditLog.Action)
            .ToListAsync();
        actions.Should().Contain("UnitAdjustmentCreated");
        actions.Should().Contain("UnitAdjustmentApproved");
        actions.Should().Contain("UnitMovementPosted");
    }

    [Fact]
    public async Task Adjustment_RequestedBySameUserCannotBeApprovedByRequester()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync();
        var scheme = await CreateActiveSchemeFixtureAsync();
        var createResponse = await client.PostAsJsonAsync("/api/unit-register/adjustments", AdjustmentRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 25m, new DateOnly(2026, 5, 3)));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var adjustment = await ReadResponseAsync<UnitAdjustmentDto>(createResponse);

        var approveResponse = await client.PostAsJsonAsync($"/api/unit-register/adjustments/{adjustment.Id}/approve", new ApproveUnitAdjustmentRequest("Self approval attempt."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await approveResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Segregation of duties");
    }

    [Fact]
    public async Task HistoricalHoldings_ReconstructByValuationDateAndTransactionReference()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync();
        var scheme = await CreateActiveSchemeFixtureAsync();
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("historical-unit-approver"), "Historical Unit Approver", "Approver123!");

        var first = await CreateAdjustmentAsync(client, investor.Id, scheme.SchemeId, scheme.ClassId, 100m, new DateOnly(2026, 5, 1));
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var firstApproval = await client.PostAsJsonAsync($"/api/unit-register/adjustments/{first.Id}/approve", new ApproveUnitAdjustmentRequest("Approve first movement."));
        firstApproval.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsBootstrapAdminAsync(client);
        var second = await CreateAdjustmentAsync(client, investor.Id, scheme.SchemeId, scheme.ClassId, -40m, new DateOnly(2026, 5, 10));
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var secondApproval = await client.PostAsJsonAsync($"/api/unit-register/adjustments/{second.Id}/approve", new ApproveUnitAdjustmentRequest("Approve second movement."));
        secondApproval.StatusCode.Should().Be(HttpStatusCode.OK);

        var earlyResponse = await client.GetAsync($"/api/unit-register/investors/{investor.Id}/historical?date=2026-05-05");
        earlyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var earlyHoldings = await ReadResponseAsync<IReadOnlyCollection<HistoricalHoldingDto>>(earlyResponse);
        earlyHoldings.Should().ContainSingle();
        earlyHoldings.Single().Units.Should().Be(100m);
        earlyHoldings.Single().TransactionReference.Should().Be(first.TransactionReference);

        var laterResponse = await client.GetAsync($"/api/unit-register/investors/{investor.Id}/historical?date=2026-05-12");
        laterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var laterHoldings = await ReadResponseAsync<IReadOnlyCollection<HistoricalHoldingDto>>(laterResponse);
        laterHoldings.Should().ContainSingle();
        laterHoldings.Single().Units.Should().Be(60m);
        laterHoldings.Single().TransactionReference.Should().Be(second.TransactionReference);
    }

    [Fact]
    public async Task CreateAdjustment_WithSameIdempotencyKeyAndSamePayload_ReturnsOriginalAdjustment()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync();
        var scheme = await CreateActiveSchemeFixtureAsync();
        var idempotencyKey = $"unit-adjustment-{Guid.NewGuid():N}";
        var request = AdjustmentRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 75m, new DateOnly(2026, 5, 4));

        var firstResponse = await PostAdjustmentWithIdempotencyKeyAsync(client, request, idempotencyKey);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var first = await ReadResponseAsync<UnitAdjustmentDto>(firstResponse);

        var replayResponse = await PostAdjustmentWithIdempotencyKeyAsync(client, request, idempotencyKey);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var replay = await ReadResponseAsync<UnitAdjustmentDto>(replayResponse);

        replay.Id.Should().Be(first.Id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var adjustments = await dbContext.UnitAdjustments.Where(candidate => candidate.IdempotencyKey == idempotencyKey).ToListAsync();
        adjustments.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateAdjustment_WithSameIdempotencyKeyAndDifferentPayload_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync();
        var scheme = await CreateActiveSchemeFixtureAsync();
        var idempotencyKey = $"unit-adjustment-{Guid.NewGuid():N}";

        var firstRequest = AdjustmentRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 55m, new DateOnly(2026, 5, 4));
        var firstResponse = await PostAdjustmentWithIdempotencyKeyAsync(client, firstRequest, idempotencyKey);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var conflictingRequest = firstRequest with { Units = 60m, TransactionReference = $"ADJ-{Guid.NewGuid():N}"[..30] };
        var replayResponse = await PostAdjustmentWithIdempotencyKeyAsync(client, conflictingRequest, idempotencyKey);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        replayResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private static CreateUnitAdjustmentRequest AdjustmentRequest(Guid investorId, Guid schemeId, Guid classId, decimal units, DateOnly valuationDate)
    {
        return new CreateUnitAdjustmentRequest(
            investorId,
            schemeId,
            classId,
            units,
            valuationDate,
            $"ADJ-{Guid.NewGuid():N}"[..30],
            6,
            "Fixture unit adjustment.");
    }

    private async Task<UnitAdjustmentDto> CreateAdjustmentAsync(HttpClient client, Guid investorId, Guid schemeId, Guid classId, decimal units, DateOnly valuationDate)
    {
        var response = await client.PostAsJsonAsync("/api/unit-register/adjustments", AdjustmentRequest(investorId, schemeId, classId, units, valuationDate));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return await ReadResponseAsync<UnitAdjustmentDto>(response);
    }

    private static Task<HttpResponseMessage> PostAdjustmentWithIdempotencyKeyAsync(HttpClient client, CreateUnitAdjustmentRequest request, string idempotencyKey)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/unit-register/adjustments")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add(StandardHeaders.IdempotencyKey, idempotencyKey);
        return client.SendAsync(message);
    }

    private async Task<(Guid Id, string InvestorNumber)> CreateInvestorFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-UNIT-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Approved Unit Investor", UniqueEmail("unit-investor"), $"+2547{Random.Shared.Next(10000000, 99999999)}", "fixture-maker", now);
        investor.AttachIndividualProfile("Jane", "Unit", $"ID-{Guid.NewGuid():N}"[..20], new DateOnly(1990, 1, 1), "Kenyan");
        investor.AddDefaultKycRequirements(["NationalId", "TaxCertificate", "ProofOfAddress"]);
        investor.SetTaxProfile($"TAX-{Guid.NewGuid():N}"[..20], "Kenya");
        investor.AssignRiskClassification(InvestorRiskCategory.Low, "Fixture risk.", "fixture-maker", now);
        foreach (var documentType in new[] { "NationalId", "TaxCertificate", "ProofOfAddress" })
        {
            investor.AddDocument(
                documentType,
                $"{documentType}.pdf",
                "application/pdf",
                2000,
                $"fixture/{Guid.NewGuid():N}.pdf",
                BusinessDate.From(new DateOnly(2026, 1, 1)),
                BusinessDate.From(new DateOnly(2030, 1, 1)),
                "fixture-maker",
                now,
                BusinessDate.From(new DateOnly(2026, 5, 12)));
        }

        investor.AddAmlScreeningCase("Fixture", $"AML-{Guid.NewGuid():N}"[..20], [], now);
        investor.SubmitKyc("fixture-maker", now);
        investor.Approve("fixture-approver", now.AddMinutes(1), BusinessDate.From(new DateOnly(2026, 5, 12)), "Fixture approved.");
        dbContext.Investors.Add(investor);
        await dbContext.SaveChangesAsync();
        return (investor.Id, investor.InvestorNumber);
    }

    private async Task<(Guid SchemeId, Guid ClassId)> CreateActiveSchemeFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"UR{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Scheme.Create(code, $"Unit Register Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", "AUM", 1m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddBankAccount("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
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

    private static async Task<UserDto> CreateUserAsync(HttpClient client, string email, string displayName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(email, displayName, password));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<UserDto>(response);
    }

    private async Task<(string Email, string Password)> CreateSystemAdminDirectlyAsync(string email, string displayName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var role = await dbContext.Roles.SingleAsync(candidate => candidate.Name == RoleNames.SystemAdmin);
        var now = DateTime.UtcNow;
        var user = User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (email, password);
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
