using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.NAV;
using Cis.Domain.NAV;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.NAV;

[Collection(IntegrationTestCollection.Name)]
public sealed class NavModuleTests
{
    private static readonly DateOnly ValuationDate = new(2026, 5, 12);
    private readonly CisApiFactory _factory;

    public NavModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedNavEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/nav/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedNavEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("nav-portal"), "Portal User", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.PostAsJsonAsync("/api/nav/valuation-runs", ValuationRequest(Guid.NewGuid(), Guid.NewGuid(), [InstrumentInput(Guid.NewGuid())]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ValuationRun_WithApprovalChain_PublishesReconstructsAuditsAndKeepsPublicationImmutable()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var instrument = await CreateInstrumentFixtureAsync("NAV-WORKFLOW-001", "NAV Workflow Treasury Bill", "TreasuryBill", "KES");

        var createResponse = await client.PostAsJsonAsync("/api/nav/valuation-runs", ValuationRequest(scheme.SchemeId, scheme.ClassId, [InstrumentInput(instrument.Id)]));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadResponseAsync<ValuationRunDto>(createResponse);
        created.Status.Should().Be("Draft");

        var calculateResponse = await client.PostAsync($"/api/nav/valuation-runs/{created.Id}/calculate", null);
        calculateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var calculated = await ReadResponseAsync<ValuationRunDto>(calculateResponse);
        calculated.Status.Should().Be("Calculated");
        calculated.Calculation.Should().NotBeNull();
        calculated.NavPerUnit.Should().NotBeNull();

        var submitResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/submit", new NavWorkflowActionRequest("Prepared NAV."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("nav-checker"), "NAV Checker", "Checker123!");
        await AuthenticateAsync(client, checker.Email, checker.Password);
        var checkResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/check", new NavWorkflowActionRequest("Checked NAV."));
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("nav-approver"), "NAV Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/approve", new NavWorkflowActionRequest("Approved NAV."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/publish", new NavWorkflowActionRequest("Published NAV."));
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publication = await ReadResponseAsync<NavPublicationDto>(publishResponse);
        publication.VersionNumber.Should().Be(1);
        publication.PublishedNav.Should().BeGreaterThan(0m);

        var historyResponse = await client.GetAsync($"/api/nav/history?schemeId={scheme.SchemeId}&schemeClassId={scheme.ClassId}");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await ReadResponseAsync<IReadOnlyCollection<NavPublicationDto>>(historyResponse);
        history.Should().Contain(item => item.Id == publication.Id);

        var reconstructResponse = await client.GetAsync($"/api/nav/reconstruct?schemeId={scheme.SchemeId}&valuationDate={ValuationDate:yyyy-MM-dd}");
        reconstructResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reconstruction = await ReadResponseAsync<NavReconstructionDto>(reconstructResponse);
        reconstruction.Publications.Should().ContainSingle(item => item.Id == publication.Id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(log => log.EntityId == created.Id.ToString() || log.EntityId == publication.Id.ToString())
            .Select(log => log.Action)
            .ToListAsync();
        actions.Should().Contain(["ValuationRunCreated", "ValuationRunCalculated", "ValuationRunSubmitted", "ValuationRunChecked", "ValuationRunApproved", "NavPublished"]);

        var publicationEntity = await dbContext.NavPublications.FirstAsync(item => item.Id == publication.Id);
        dbContext.Entry(publicationEntity).Property(nameof(NavPublication.PublishedNav)).CurrentValue = publication.PublishedNav + 1m;
        dbContext.Entry(publicationEntity).State = EntityState.Modified;
        var act = () => dbContext.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*immutable*");
    }

    [Fact]
    public async Task PublishBeforeApproval_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var instrument = await CreateInstrumentFixtureAsync("NAV-PUBLISH-001", "NAV Publish Treasury Bill", "TreasuryBill", "KES");
        var created = await CreateAndCalculateRunAsync(client, scheme, instrument.Id);

        var publishResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/publish", new NavWorkflowActionRequest("Too early."));

        publishResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        publishResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await publishResponse.Content.ReadAsStringAsync();
        body.Should().Contain("approved");
    }

    [Fact]
    public async Task SameUserCannotCheckSubmittedNavWorkflow()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var instrument = await CreateInstrumentFixtureAsync("NAV-SOD-001", "NAV SoD Treasury Bill", "TreasuryBill", "KES");
        var created = await CreateAndCalculateRunAsync(client, scheme, instrument.Id);
        var submitResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/submit", new NavWorkflowActionRequest("Submit NAV."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checkResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{created.Id}/check", new NavWorkflowActionRequest("Self check."));

        checkResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await checkResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Segregation of duties");
    }

    [Fact]
    public async Task MissingAndStalePrices_CreatePriceExceptions()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var missingPriceInstrument = await CreateInstrumentFixtureAsync("NAV-MISSING-001", "Missing Price Bill", "TreasuryBill", "KES");
        var stalePriceInstrument = await CreateInstrumentFixtureAsync("NAV-STALE-001", "Stale Price Bill", "TreasuryBill", "KES");

        var request = ValuationRequest(
            scheme.SchemeId,
            scheme.ClassId,
            [
                InstrumentInput(missingPriceInstrument.Id, marketPrice: null, priceDate: null),
                InstrumentInput(stalePriceInstrument.Id, marketPrice: 100m, priceDate: ValuationDate.AddDays(-10))
            ],
            priceStaleAfterDays: 2);

        var response = await client.PostAsJsonAsync("/api/nav/valuation-runs", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<ValuationRunDto>(response);
        run.StalePriceExceptions.Should().HaveCount(2);
        run.StalePriceExceptions.Should().Contain(item => item.MissingPrice);
        run.StalePriceExceptions.Should().Contain(item => !item.MissingPrice);
    }

    [Fact]
    public async Task ManualOverride_AffectsNavOnlyAfterApproval()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var instrument = await CreateInstrumentFixtureAsync("NAV-OVERRIDE-001", "Override Price Bill", "TreasuryBill", "KES");

        var overrideResponse = await client.PostAsJsonAsync("/api/nav/overrides", new CreateManualValuationOverrideRequest(
            scheme.SchemeId,
            scheme.ClassId,
            ValuationDate,
            instrument.Id,
            105m,
            105_000m,
            "Quoted market source unavailable."));
        overrideResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var manualOverride = await ReadResponseAsync<ManualValuationOverrideDto>(overrideResponse);

        var pendingRunResponse = await client.PostAsJsonAsync("/api/nav/valuation-runs", ValuationRequest(scheme.SchemeId, scheme.ClassId, [InstrumentInput(instrument.Id, marketPrice: null, priceDate: null)]));
        pendingRunResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pendingRun = await ReadResponseAsync<ValuationRunDto>(pendingRunResponse);
        pendingRun.Instruments.Single().OverrideApplied.Should().BeFalse();
        pendingRun.Instruments.Single().InvestmentValue.Should().Be(0m);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("nav-override-approver"), "Override Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/nav/overrides/{manualOverride.Id}/approve", new ApproveManualValuationOverrideRequest("Approved override."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedRunResponse = await client.PostAsJsonAsync("/api/nav/valuation-runs", ValuationRequest(scheme.SchemeId, scheme.ClassId, [InstrumentInput(instrument.Id, marketPrice: null, priceDate: null)]));
        approvedRunResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var approvedRun = await ReadResponseAsync<ValuationRunDto>(approvedRunResponse);
        approvedRun.Instruments.Single().OverrideApplied.Should().BeTrue();
        approvedRun.Instruments.Single().InvestmentValue.Should().Be(105_000m);
        approvedRun.StalePriceExceptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Restatement_CreatesCorrectedPublishedVersionAndReconstructionUsesLatest()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var instrument = await CreateInstrumentFixtureAsync("NAV-RESTATED-001", "Restated NAV Bill", "TreasuryBill", "KES");

        var originalRun = await CreateApprovedRunAsync(client, scheme, instrument.Id, 100m, "original");
        var originalPublicationResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{originalRun.Id}/publish", new NavWorkflowActionRequest("Original publication."));
        originalPublicationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var originalPublication = await ReadResponseAsync<NavPublicationDto>(originalPublicationResponse);

        var correctedRun = await CreateApprovedRunAsync(client, scheme, instrument.Id, 110m, "corrected");
        var restatementResponse = await client.PostAsJsonAsync("/api/nav/restatements", new CreateNavRestatementRequest(
            originalPublication.Id,
            correctedRun.Id,
            "Corrected stale broker price."));

        restatementResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var restatement = await ReadResponseAsync<NavRestatementDto>(restatementResponse);
        restatement.CorrectedVersionNumber.Should().Be(2);

        var reconstructionResponse = await client.GetAsync($"/api/nav/reconstruct?schemeId={scheme.SchemeId}&valuationDate={ValuationDate:yyyy-MM-dd}");
        reconstructionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reconstruction = await ReadResponseAsync<NavReconstructionDto>(reconstructionResponse);
        reconstruction.Publications.Should().ContainSingle();
        reconstruction.Publications.Single().VersionNumber.Should().Be(2);
    }

    private async Task<ValuationRunDto> CreateAndCalculateRunAsync(HttpClient client, (Guid SchemeId, Guid ClassId) scheme, Guid instrumentId, decimal price = 102m)
    {
        var createResponse = await client.PostAsJsonAsync("/api/nav/valuation-runs", ValuationRequest(scheme.SchemeId, scheme.ClassId, [InstrumentInput(instrumentId, marketPrice: price)]));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<ValuationRunDto>(createResponse);
        var calculateResponse = await client.PostAsync($"/api/nav/valuation-runs/{run.Id}/calculate", null);
        calculateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<ValuationRunDto>(calculateResponse);
    }

    private async Task<ValuationRunDto> CreateApprovedRunAsync(HttpClient client, (Guid SchemeId, Guid ClassId) scheme, Guid instrumentId, decimal price, string suffix)
    {
        await AuthenticateAsBootstrapAdminAsync(client);
        var run = await CreateAndCalculateRunAsync(client, scheme, instrumentId, price);
        var submitResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/submit", new NavWorkflowActionRequest($"Submit {suffix}."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail($"nav-{suffix}-checker"), $"NAV {suffix} Checker", "Checker123!");
        await AuthenticateAsync(client, checker.Email, checker.Password);
        var checkResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/check", new NavWorkflowActionRequest($"Check {suffix}."));
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail($"nav-{suffix}-approver"), $"NAV {suffix} Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/approve", new NavWorkflowActionRequest($"Approve {suffix}."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<ValuationRunDto>(approveResponse);
    }

    private static CreateValuationRunRequest ValuationRequest(
        Guid schemeId,
        Guid classId,
        IReadOnlyCollection<InstrumentValuationInputRequest> instruments,
        int priceStaleAfterDays = 5)
    {
        return new CreateValuationRunRequest(
            schemeId,
            classId,
            ValuationDate,
            "NAV-FORMULA-V1",
            DayCountBasis.Actual365.ToString(),
            4,
            priceStaleAfterDays,
            5m,
            false,
            10_000m,
            500m,
            100m,
            2_000m,
            300m,
            100_000m,
            1_000m,
            500m,
            0m,
            1_000m,
            10m,
            5m,
            100m,
            1m,
            2m,
            1m,
            50m,
            1m,
            50m,
            1m,
            1_000m,
            1m,
            5m,
            2m,
            instruments);
    }

    private static InstrumentValuationInputRequest InstrumentInput(
        Guid instrumentId,
        decimal? marketPrice = 102m,
        DateOnly? priceDate = null,
        decimal? priorPrice = 101m)
    {
        return new InstrumentValuationInputRequest(
            instrumentId,
            "TreasuryBill",
            1_000m,
            marketPrice,
            priceDate ?? ValuationDate,
            100_000m,
            7.3m,
            30,
            98_000m,
            100_000m,
            90,
            null,
            false,
            priorPrice,
            "Bloomberg");
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

    private async Task<(Guid Id, string Email, string Password)> CreateSystemAdminDirectlyAsync(string email, string displayName, string password)
    {
        return await CreateUserWithRoleDirectlyAsync(email, displayName, password, RoleNames.SystemAdmin);
    }

    private async Task<(Guid Id, string Email, string Password)> CreateUserWithRoleDirectlyAsync(string email, string displayName, string password, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Cis.Domain.Identity.User>>();
        var role = await dbContext.Roles.SingleAsync(role => role.Name == roleName);
        var now = DateTime.UtcNow;
        var user = Cis.Domain.Identity.User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private async Task<(Guid SchemeId, Guid ClassId)> CreateActiveSchemeFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"NAV{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Cis.Domain.Schemes.Scheme.Create(code, $"NAV Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", "AUM", 1m, null, Cis.Domain.Common.BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddBankAccount("Victory Bank", $"0200{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
    }

    private async Task<Cis.Domain.Portfolio.Instrument> CreateInstrumentFixtureAsync(string isin, string name, string instrumentType, string currency)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var instrument = Cis.Domain.Portfolio.Instrument.Create(isin, name, Enum.Parse<Cis.Domain.Portfolio.InstrumentType>(instrumentType), currency, "fixture-maker", DateTime.UtcNow);
        dbContext.Instruments.Add(instrument);
        await dbContext.SaveChangesAsync();
        return instrument;
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@test.local";
    }
}
