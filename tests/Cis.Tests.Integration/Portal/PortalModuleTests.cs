using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Portal;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Portal;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Portal;

[Collection(IntegrationTestCollection.Name)]
public sealed class PortalModuleTests
{
    private readonly CisApiFactory _factory;

    public PortalModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PortalEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/portal/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PortalEndpoint_WithRoleWithoutPortalPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var user = await CreateUserWithRoleDirectlyAsync(UniqueEmail("portal-board"), "Portal Board", "Portal123!", RoleNames.BoardUser);
        await AuthenticateAsync(client, user.Email, user.Password);

        var response = await client.GetAsync("/api/portal/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PortalAccess_WhenMfaRequiredAndNotSatisfied_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var seed = await SeedPortalInvestorAsync("MFA", mfaRequired: true, mfaVerified: false, seedHolding: true);
        await AuthenticateAsync(client, seed.Email, seed.Password);

        var response = await client.GetAsync("/api/portal/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PortalQueries_AreScopedToOwnInvestorAndLogActivityAndDownloads()
    {
        using var client = _factory.CreateClient();
        var own = await SeedPortalInvestorAsync("OWN", mfaRequired: true, mfaVerified: true, seedHolding: true, seedTax: true);
        var other = await SeedPortalInvestorAsync("OTHER", mfaRequired: false, mfaVerified: false, seedHolding: true, seedTax: true);
        await AuthenticateAsync(client, own.Email, own.Password);

        var meResponse = await client.GetAsync("/api/portal/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await ReadResponseAsync<PortalProfileDto>(meResponse);
        me.InvestorId.Should().Be(own.InvestorId);
        me.MfaRequired.Should().BeTrue();
        me.MfaSatisfied.Should().BeTrue();

        var holdingsResponse = await client.GetAsync("/api/portal/holdings");
        holdingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var holdings = await ReadResponseAsync<IReadOnlyCollection<PortalHoldingDto>>(holdingsResponse);
        holdings.Should().ContainSingle();
        holdings.Single().Units.Should().Be(125.25m);

        var transactionsResponse = await client.GetAsync("/api/portal/transactions");
        transactionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var transactions = await ReadResponseAsync<IReadOnlyCollection<PortalTransactionDto>>(transactionsResponse);
        transactions.Should().Contain(transaction => transaction.Reference == own.TransactionReference);
        transactions.Should().NotContain(transaction => transaction.Reference == other.TransactionReference);

        var statementsResponse = await client.GetAsync("/api/portal/statements");
        statementsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statements = await ReadResponseAsync<IReadOnlyCollection<PortalStatementDto>>(statementsResponse);
        statements.Should().ContainSingle();
        statements.Single().InvestorId.Should().Be(own.InvestorId);

        var taxResponse = await client.GetAsync("/api/portal/tax-certificates");
        taxResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var taxCertificates = await ReadResponseAsync<IReadOnlyCollection<PortalTaxCertificateDto>>(taxResponse);
        taxCertificates.Should().ContainSingle();
        taxCertificates.Single().InvestorId.Should().Be(own.InvestorId);

        var noticesResponse = await client.GetAsync("/api/portal/notices");
        noticesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var notices = await ReadResponseAsync<IReadOnlyCollection<InvestorNoticeDto>>(noticesResponse);
        notices.Should().Contain(notice => notice.Title.Contains("General notice"));
        notices.Should().Contain(notice => notice.Title.Contains("OWN notice"));
        notices.Should().NotContain(notice => notice.Title.Contains("OTHER notice"));

        var activityResponse = await client.GetAsync("/api/portal/activity");
        activityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activity = await ReadResponseAsync<IReadOnlyCollection<PortalActivityLogDto>>(activityResponse);
        activity.Should().Contain(log => log.ActivityType == PortalActivityType.ViewedHoldings.ToString());
        activity.Should().Contain(log => log.ActivityType == PortalActivityType.DownloadedStatement.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.PortalDocumentDownloads.CountAsync(download => download.InvestorId == own.InvestorId)).Should().Be(2);
        (await dbContext.PortalDocumentDownloads.AnyAsync(download => download.InvestorId == other.InvestorId)).Should().BeFalse();
        (await dbContext.PortalActivityLogs.CountAsync(log => log.InvestorId == own.InvestorId)).Should().BeGreaterThan(0);
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Portal" && log.Action == "PortalStatementDownloaded" && log.EventType == AuditEventType.Exported)).Should().BeTrue();
    }

    [Fact]
    public async Task DigitalRequests_CreateWorkflowBackedInternalRequestWithoutChangingOfficialRecords()
    {
        using var client = _factory.CreateClient();
        var seed = await SeedPortalInvestorAsync("REQ", mfaRequired: false, mfaVerified: false, seedHolding: true);
        await AuthenticateAsync(client, seed.Email, seed.Password);

        var invalidRedemption = await client.PostAsJsonAsync("/api/portal/requests/redemption", new CreatePortalRedemptionRequest(seed.SchemeId, seed.SchemeClassId, null, null, false, "KES"));
        invalidRedemption.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var response = await client.PostAsJsonAsync("/api/portal/requests/subscription", new CreatePortalSubscriptionRequest(seed.SchemeId, seed.SchemeClassId, 1_000m, "KES"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var digitalRequest = await ReadResponseAsync<DigitalServiceRequestDto>(response);
        digitalRequest.InvestorId.Should().Be(seed.InvestorId);
        digitalRequest.RequestType.Should().Be(DigitalServiceRequestType.SubscriptionRequest.ToString());
        digitalRequest.Status.Should().Be(DigitalServiceRequestStatus.WorkflowPending.ToString());
        digitalRequest.WorkflowId.Should().NotBeNull();

        var documentResponse = await client.PostAsJsonAsync("/api/portal/documents", new UploadPortalDocumentRequest("ProofOfPayment", "proof.pdf", "application/pdf", 2048, "portal/proof.pdf"));
        documentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var documentRequest = await ReadResponseAsync<DigitalServiceRequestDto>(documentResponse);
        documentRequest.RequestType.Should().Be(DigitalServiceRequestType.DocumentUploadRequest.ToString());
        documentRequest.WorkflowId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var persisted = await dbContext.DigitalServiceRequests.SingleAsync(request => request.Id == digitalRequest.Id);
        persisted.WorkflowId.Should().Be(digitalRequest.WorkflowId);
        (await dbContext.WorkflowInstances.AnyAsync(workflow => workflow.Id == digitalRequest.WorkflowId && workflow.WorkflowType == WorkflowType.PortalDigitalServiceRequest)).Should().BeTrue();
        (await dbContext.DealingInstructions.AnyAsync(instruction => instruction.InvestorId == seed.InvestorId)).Should().BeFalse();
        (await dbContext.PortalActivityLogs.AnyAsync(log => log.InvestorId == seed.InvestorId && log.ActivityType == PortalActivityType.SubmittedDigitalRequest)).Should().BeTrue();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Portal" && log.Action == "PortalDigitalServiceRequestSubmitted" && log.EntityId == digitalRequest.Id.ToString())).Should().BeTrue();
    }

    private async Task<PortalSeed> SeedPortalInvestorAsync(string prefix, bool mfaRequired, bool mfaVerified, bool seedHolding, bool seedTax = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var user = await CreateUserWithRoleDirectlyAsync(UniqueEmail($"portal-{prefix.ToLowerInvariant()}"), $"{prefix} Portal User", "Portal123!", RoleNames.InvestorPortalUser);
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-{prefix}-{Guid.NewGuid():N}"[..30], InvestorType.Individual, $"{prefix} Investor", user.Email, "+254700000000", "test-fixture", now);
        if (seedTax)
        {
            investor.SetTaxProfile($"TAX-{prefix}", "KE");
        }
        var scheme = Scheme.Create($"SCH-{prefix}-{Guid.NewGuid():N}"[..30], $"{prefix} Scheme", "Unit Trust", "KES", "test-fixture", now);
        var schemeClass = scheme.AddClass("CLS", "Default Class", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(15, 0), 100m, 10m, 0, 0);
        dbContext.Investors.Add(investor);
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();

        var profile = PortalUserProfile.Create(user.Id, investor.Id, investor.DisplayName, investor.Email, "test-fixture", now);
        var mfa = PortalMfaSetting.Create(user.Id, mfaRequired, mfaVerified, "test-fixture", now);
        dbContext.PortalUserProfiles.Add(profile);
        dbContext.PortalMfaSettings.Add(mfa);
        dbContext.InvestorNotices.Add(InvestorNotice.Create(null, $"General notice {prefix}", "General notice body.", BusinessDate.From(new DateOnly(2026, 10, 1)), now, "test-fixture"));
        dbContext.InvestorNotices.Add(InvestorNotice.Create(investor.Id, $"{prefix} notice", "Investor-specific notice body.", BusinessDate.From(new DateOnly(2026, 10, 2)), now, "test-fixture"));

        var transactionReference = $"PORTAL-{prefix}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        if (seedHolding)
        {
            var source = UnitMovementSource.Create(UnitMovementSourceType.Adjustment, Guid.NewGuid(), investor.Id, scheme.Id, schemeClass.Id, transactionReference, true, "test-fixture", now);
            var entry = UnitLedgerEntry.Create(UnitMovementType.Issued, investor.Id, scheme.Id, schemeClass.Id, BusinessDate.From(new DateOnly(2026, 10, 1)), 125.25m, source, transactionReference, 4, "test-fixture", now, "Portal seed holding.");
            var holding = UnitHolding.Create(investor.Id, scheme.Id, schemeClass.Id, 4, "test-fixture", now);
            holding.Apply(entry);
            dbContext.UnitMovementSources.Add(source);
            dbContext.UnitLedgerEntries.Add(entry);
            dbContext.UnitHoldings.Add(holding);
        }

        await dbContext.SaveChangesAsync();
        return new PortalSeed(user.Id, user.Email, user.Password, investor.Id, scheme.Id, schemeClass.Id, transactionReference);
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

    private static async Task<AuthTokenResponse> AuthenticateAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var token = await ReadResponseAsync<AuthTokenResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return token;
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

    private sealed record PortalSeed(Guid UserId, string Email, string Password, Guid InvestorId, Guid SchemeId, Guid SchemeClassId, string TransactionReference);
}
