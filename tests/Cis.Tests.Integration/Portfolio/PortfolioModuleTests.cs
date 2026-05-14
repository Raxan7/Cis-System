using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Portfolio;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Portfolio;

[Collection(IntegrationTestCollection.Name)]
public sealed class PortfolioModuleTests
{
    private readonly CisApiFactory _factory;

    public PortfolioModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedPortfolioEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/portfolio/holdings?schemeId=00000000-0000-0000-0000-000000000000&schemeClassId=00000000-0000-0000-0000-000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateInstrument_WithValidRequest_ReturnsInstrumentAndAuditsCreation()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/portfolio/instruments", new CreateInstrumentRequest(
            "US0378331005",
            "Treasury Bill 6M",
            "TreasuryBill",
            "USD",
            null,
            null,
            2.5m,
            null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var instrument = await ReadResponseAsync<InstrumentDto>(response);
        instrument.Isin.Should().Be("US0378331005");
        instrument.Name.Should().Be("Treasury Bill 6M");
        instrument.Currency.Should().Be("USD");
        instrument.YieldRate.Should().Be(2.5m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var auditLog = await dbContext.AuditLogs
            .Where(log => log.EntityId == instrument.Id.ToString() && log.Action == "InstrumentCreated")
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCounterparty_WithValidRequest_ReturnsCounterpartyDto()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/portfolio/counterparties", new CreateCounterpartyRequest(
            "CPTY001",
            "Primary Bank Ltd.",
            "contact@primarybank.local"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var counterparty = await ReadResponseAsync<CounterpartyDto>(response);
        counterparty.Code.Should().Be("CPTY001");
        counterparty.Name.Should().Be("Primary Bank Ltd.");
        counterparty.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Placement_WithValidInstrumentAndScheme_CanBeSubmittedAndApproved()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateActiveSchemeFixtureAsync(_factory);
        var instrument = await CreateInstrumentFixtureAsync(_factory, "GB0002374006", "Bond 10Y", "TreasuryBond", "GBP");

        var createResponse = await client.PostAsJsonAsync("/api/portfolio/placements", new CreatePlacementRequest(
            scheme.SchemeId,
            scheme.ClassId,
            instrument.Id,
            null,
            null,
            1_000_000m,
            "GBP",
            new DateOnly(2026, 5, 12),
            new DateOnly(2036, 5, 12),
            3.5m,
            15_000m));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var placement = await ReadResponseAsync<PlacementDto>(createResponse);
        placement.Status.Should().Be("Draft");
        placement.Principal.Should().Be(1_000_000m);

        // Submit placement
        var submitResponse = await client.PostAsJsonAsync($"/api/portfolio/placements/{placement.Id}/submit", new PlacementSubmitRequest());
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await ReadResponseAsync<PlacementDto>(submitResponse);
        submitted.Status.Should().Be("PendingApproval");

        // Approve placement
        var approver = await CreateSystemAdminDirectlyAsync(_factory, "portfolio-approver@test.local", "Portfolio Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/portfolio/placements/{placement.Id}/approve", new PlacementApproveRequest(null));
        
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await ReadResponseAsync<PlacementDto>(approveResponse);
        approved.Status.Should().Be("Approved");
        approved.SettlementStatus.Should().Be("Settled");
        approved.ApprovedByUserId.Should().NotBeNullOrWhiteSpace();
        approved.IncomeSchedules.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task PlacementWithTenorViolation_ExceedingMandateLimit_IsRejected()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateActiveSchemeFixtureAsync(_factory);
        var instrument = await CreateInstrumentFixtureAsync(_factory, "BOND-TENOR", "Bond Tenor Test", "TreasuryBond", "KES");

        var createResponse = await client.PostAsJsonAsync("/api/portfolio/placements", new CreatePlacementRequest(
            scheme.SchemeId,
            scheme.ClassId,
            instrument.Id,
            null,
            null,
            500_000m,
            "KES",
            new DateOnly(2026, 5, 12),
            new DateOnly(2046, 5, 12), // 20 years, exceeds 365-day mandate limit
            2m,
            0m));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var placement = await ReadResponseAsync<PlacementDto>(createResponse);

        var submitResponse = await client.PostAsJsonAsync($"/api/portfolio/placements/{placement.Id}/submit", new PlacementSubmitRequest());
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(_factory, "tenor-approver@test.local", "Tenor Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/portfolio/placements/{placement.Id}/approve", new PlacementApproveRequest(null));
        
        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemBody = await approveResponse.Content.ReadAsStringAsync();
        problemBody.Should().ContainAny("tenor", "Tenor").And.ContainAny("exceeds", "Exceeds");
    }

    [Fact]
    public async Task GetMaturityLadder_ReturnsUpcomingMaturitiesBySchemeClassSortedByDate()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateActiveSchemeFixtureAsync(_factory);
        var instrument1 = await CreateInstrumentFixtureAsync(_factory, "MAT-001", "Maturity Test 1", "TreasuryBill", "KES");
        var instrument2 = await CreateInstrumentFixtureAsync(_factory, "MAT-002", "Maturity Test 2", "TreasuryBond", "KES");

        // Create and approve first placement
        var placement1 = await CreateAndApprovePlacementFixtureAsync(_factory, scheme.SchemeId, scheme.ClassId, instrument1.Id, 100_000m, "KES", new DateOnly(2026, 6, 1), "portfolio-approver-1@test.local");

        // Create and approve second placement with later maturity
        var placement2 = await CreateAndApprovePlacementFixtureAsync(_factory, scheme.SchemeId, scheme.ClassId, instrument2.Id, 200_000m, "KES", new DateOnly(2026, 7, 1), "portfolio-approver-2@test.local");

        var response = await client.GetAsync($"/api/portfolio/maturity-ladder?schemeId={scheme.SchemeId}&schemeClassId={scheme.ClassId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ladder = await ReadResponseAsync<MaturityLadderDto>(response);
        ladder.Entries.Should().HaveCount(2);
        ladder.Entries.ElementAt(0).MaturityDate.Should().BeBefore(ladder.Entries.ElementAt(1).MaturityDate);
    }

    [Fact]
    public async Task GetIncomeDue_ReturnsScheduledIncomeForSchemeClass()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateActiveSchemeFixtureAsync(_factory);
        var instrument = await CreateInstrumentFixtureAsync(_factory, "INCOME-TEST", "Income Test Bond", "Bond", "KES");

        var placement = await CreateAndApprovePlacementFixtureAsync(_factory, scheme.SchemeId, scheme.ClassId, instrument.Id, 1_000_000m, "KES", new DateOnly(2026, 11, 12), "portfolio-approver-income@test.local");

        var response = await client.GetAsync($"/api/portfolio/income-due?schemeId={scheme.SchemeId}&schemeClassId={scheme.ClassId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var incomeDue = await ReadResponseAsync<IncomeDueDto>(response);
        incomeDue.Entries.Should().HaveCountGreaterThan(0);
        incomeDue.TotalDue.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task RecordIncomeReceipt_MarksCouponAsReceived()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateActiveSchemeFixtureAsync(_factory);
        var instrument = await CreateInstrumentFixtureAsync(_factory, "RECEIPT-TEST", "Receipt Test", "Bond", "KES");
        var placement = await CreateAndApprovePlacementFixtureAsync(_factory, scheme.SchemeId, scheme.ClassId, instrument.Id, 500_000m, "KES", new DateOnly(2027, 5, 12), "portfolio-approver-receipt@test.local");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var schedule = await dbContext.IncomeSchedules
            .Where(s => s.PlacementId == placement.Id)
            .FirstOrDefaultAsync();

        schedule.Should().NotBeNull();

        var response = await client.PostAsJsonAsync("/api/portfolio/income-receipts", new IncomeReceiptRequest(schedule!.Id, "RECEIPT-REF-001"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var receipt = await ReadResponseAsync<IncomeScheduleDto>(response);
        receipt.Received.Should().BeTrue();
        receipt.ReceivedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRollover_CreatesNewPlacementWithExtendedMaturity()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateActiveSchemeFixtureAsync(_factory);
        var instrument = await CreateInstrumentFixtureAsync(_factory, "ROLLOVER-TEST", "Rollover Test", "TreasuryBill", "KES");
        var originalPlacement = await CreateAndApprovePlacementFixtureAsync(_factory, scheme.SchemeId, scheme.ClassId, instrument.Id, 250_000m, "KES", new DateOnly(2026, 11, 12), "portfolio-approver-rollover@test.local");

        var response = await client.PostAsJsonAsync("/api/portfolio/rollovers", new CreateRolloverRequest(
            originalPlacement.Id,
            new DateOnly(2027, 11, 12),
            null,
            "Automatic maturity rollover"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rollover = await ReadResponseAsync<RolloverEventDto>(response);
        rollover.OriginalPlacementId.Should().Be(originalPlacement.Id);
        rollover.NewMaturityDate.Should().Be(new DateOnly(2027, 11, 12));
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

    private static async Task<(Guid Id, string Email, string Password)> CreateSystemAdminDirectlyAsync(CisApiFactory factory, string email, string displayName, string password)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Cis.Domain.Identity.User>>();
        var role = await dbContext.Roles.SingleAsync(r => r.Name == "SystemAdmin");
        var now = DateTime.UtcNow;
        var user = Cis.Domain.Identity.User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private static async Task<(Guid SchemeId, Guid ClassId)> CreateActiveSchemeFixtureAsync(CisApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"PRT{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Cis.Domain.Schemes.Scheme.Create(code, $"Portfolio Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", "AUM", 1m, null, Cis.Domain.Common.BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddApprovedInstrumentRule("TreasuryBond", 365, 25m, 30m, 75m);
        scheme.AddApprovedInstrumentRule("Bond", 365, 30m, 35m, 70m);
        scheme.AddBankAccount("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
    }

    private static async Task<InstrumentDto> CreateInstrumentFixtureAsync(CisApiFactory factory, string isin, string name, string instrumentType, string currency)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var instrument = Cis.Domain.Portfolio.Instrument.Create(isin, name, Enum.Parse<Cis.Domain.Portfolio.InstrumentType>(instrumentType), currency, "fixture-maker", now);
        dbContext.Instruments.Add(instrument);
        await dbContext.SaveChangesAsync();
        return new InstrumentDto(instrument.Id, instrument.Isin, instrument.Name, instrument.InstrumentType.ToString(), instrument.Currency, instrument.Status.ToString(), instrument.CounterpartyId, instrument.IssuerId, instrument.YieldRate, instrument.CouponRate);
    }

    private static async Task<PlacementDto> CreateAndApprovePlacementFixtureAsync(CisApiFactory factory, Guid schemeId, Guid classId, Guid instrumentId, decimal principal, string currency, DateOnly maturityDate, string approverEmail)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var placement = Cis.Domain.Portfolio.Placement.Create(schemeId, classId, instrumentId, null, null, principal, currency, Cis.Domain.Common.BusinessDate.From(new DateOnly(2026, 5, 12)), Cis.Domain.Common.BusinessDate.From(maturityDate), 2.5m, 0m, "fixture-maker", now);

        if (placement.Yield > 0)
        {
            var currentDate = new DateOnly(2026, 5, 12);
            while (currentDate < maturityDate)
            {
                currentDate = currentDate.AddMonths(6);
                if (currentDate <= maturityDate)
                {
                    var couponAmount = placement.Principal * (placement.Yield / 100m) / 2m;
                    var schedule = Cis.Domain.Portfolio.IncomeSchedule.Create(placement.Id, "Coupon", Cis.Domain.Common.BusinessDate.From(currentDate), couponAmount, false, now);
                    placement.AddIncomeSchedule(schedule);
                }
            }
        }

        placement.Submit("fixture-maker", now.AddMinutes(1));
        placement.Approve(approverEmail, now.AddMinutes(2));
        dbContext.Placements.Add(placement);
        await dbContext.SaveChangesAsync();

        var incomeSchedules = await dbContext.IncomeSchedules
            .AsNoTracking()
            .Where(s => s.PlacementId == placement.Id)
            .Select(s => new IncomeScheduleDto(s.Id, s.PlacementId, s.IncomeType, s.DueDate.Value, s.Amount, s.Received, s.ReceivedAtUtc, s.ReceivedByUserId))
            .ToListAsync();

        return new PlacementDto(placement.Id, placement.SchemeId, placement.SchemeClassId, placement.InstrumentId, placement.CounterpartyId, placement.IssuerId, placement.Principal, placement.Currency, placement.AcquisitionDate.Value, placement.MaturityDate.Value, placement.Yield, placement.AccruedIncome, placement.Status.ToString(), placement.SettlementStatus.ToString(), placement.SubmittedByUserId, placement.SubmittedAtUtc, placement.ApprovedByUserId, placement.ApprovedAtUtc, incomeSchedules);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }
}
