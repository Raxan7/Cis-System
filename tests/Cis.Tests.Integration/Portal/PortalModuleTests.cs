using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Portal;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Dealing;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.NAV;
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

    [Fact]
    public async Task SelfRegistration_WithValidOtp_ActivatesPortalAccountAndCreatesDraftInvestor()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail("portal-self-register");
        const string password = "PortalSelfRegister123!";
        var phoneNumber = $"+2547{Random.Shared.Next(10000000, 99999999)}";

        var createResponse = await client.PostAsJsonAsync("/api/portal/self-registration", new CreatePortalSelfRegistrationRequest("Portal Self Investor", email, phoneNumber, password));
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted, createBody);
        var registration = await ReadResponseAsync<PortalSelfRegistrationInitiatedDto>(createResponse);
        registration.Email.Should().Be(email);
        registration.Status.Should().Be(PortalSelfRegistrationStatus.PendingVerification.ToString());

        var verifyResponse = await client.PostAsJsonAsync("/api/portal/self-registration/verify-otp", new VerifyPortalSelfRegistrationOtpRequest(registration.RegistrationId, "000000"));
        var verifyBody = await verifyResponse.Content.ReadAsStringAsync();
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK, verifyBody);
        var activation = await ReadResponseAsync<PortalSelfRegistrationActivationDto>(verifyResponse);
        activation.Email.Should().Be(email);
        activation.InvestorStatus.Should().Be(InvestorStatus.Draft.ToString());

        await AuthenticateAsync(client, email, password);
        var meResponse = await client.GetAsync("/api/portal/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await ReadResponseAsync<PortalProfileDto>(meResponse);
        me.Email.Should().Be(email);
        me.InvestorStatus.Should().Be(InvestorStatus.Draft.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var storedRegistration = await dbContext.PortalSelfRegistrations.SingleAsync(candidate => candidate.Id == registration.RegistrationId);
        storedRegistration.Status.Should().Be(PortalSelfRegistrationStatus.Activated);
        storedRegistration.UserId.Should().Be(activation.UserId);
        storedRegistration.InvestorId.Should().Be(activation.InvestorId);
        (await dbContext.Users.AnyAsync(candidate => candidate.Id == activation.UserId && candidate.Email == email)).Should().BeTrue();
        (await dbContext.PortalUserProfiles.AnyAsync(candidate => candidate.UserId == activation.UserId && candidate.InvestorId == activation.InvestorId)).Should().BeTrue();
        (await dbContext.KycRequirements.CountAsync(candidate => candidate.InvestorId == activation.InvestorId)).Should().Be(3);
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Portal" && log.Action == "PortalSelfRegistrationStarted")).Should().BeTrue();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Portal" && log.Action == "PortalSelfRegistrationActivated")).Should().BeTrue();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Identity" && log.Action == "UserCreated" && log.EntityId == activation.UserId.ToString())).Should().BeTrue();
    }

    [Fact]
    public async Task SelfRegistration_WithWrongOtp_DoesNotActivatePortalAccount()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail("portal-invalid-otp");
        const string password = "PortalSelfRegister123!";
        var phoneNumber = $"+2547{Random.Shared.Next(10000000, 99999999)}";

        var createResponse = await client.PostAsJsonAsync("/api/portal/self-registration", new CreatePortalSelfRegistrationRequest("OTP Failure Investor", email, phoneNumber, password));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var registration = await ReadResponseAsync<PortalSelfRegistrationInitiatedDto>(createResponse);

        var verifyResponse = await client.PostAsJsonAsync("/api/portal/self-registration/verify-otp", new VerifyPortalSelfRegistrationOtpRequest(registration.RegistrationId, "999999"));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.Users.AnyAsync(candidate => candidate.Email == email)).Should().BeFalse();
        var storedRegistration = await dbContext.PortalSelfRegistrations.SingleAsync(candidate => candidate.Id == registration.RegistrationId);
        storedRegistration.UserId.Should().BeNull();
        storedRegistration.InvestorId.Should().BeNull();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Portal" && log.Action == "PortalSelfRegistrationOtpRejected" && log.EntityId == registration.RegistrationId.ToString())).Should().BeTrue();
    }

    [Fact]
    public async Task SelfRegisteredDraftInvestor_CannotSubmitRedemptionUntilKycIsCompleted()
    {
        using var client = _factory.CreateClient();
        var activation = await CreateSelfRegisteredPortalAccountAsync(client, "redeem-gate");
        await AuthenticateAsync(client, activation.Email, activation.Password);
        var scheme = await CreatePortalSchemeFixtureAsync("SELFREG-REDEEM");

        var response = await client.PostAsJsonAsync("/api/portal/requests/redemption", new CreatePortalRedemptionRequest(scheme.SchemeId, scheme.ClassId, 100m, null, false, "KES"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("Complete your KYC and obtain investor approval");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.DigitalServiceRequests.AnyAsync(request => request.UserId == activation.UserId && request.RequestType == DigitalServiceRequestType.RedemptionRequest)).Should().BeFalse();
    }

    [Fact]
    public async Task SelfRegisteredDraftInvestor_CanSubmitSubscriptionRequestBeforeFullKycCompletion()
    {
        using var client = _factory.CreateClient();
        var activation = await CreateSelfRegisteredPortalAccountAsync(client, "subscription-gate");
        await AuthenticateAsync(client, activation.Email, activation.Password);
        var scheme = await CreatePortalSchemeFixtureAsync("SELFREG-SUB");

        var response = await client.PostAsJsonAsync("/api/portal/requests/subscription", new CreatePortalSubscriptionRequest(scheme.SchemeId, scheme.ClassId, 2_500m, "KES"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var request = await ReadResponseAsync<DigitalServiceRequestDto>(response);
        request.RequestType.Should().Be(DigitalServiceRequestType.SubscriptionRequest.ToString());
        request.Status.Should().Be(DigitalServiceRequestStatus.WorkflowPending.ToString());
        request.WorkflowId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.DigitalServiceRequests.AnyAsync(candidate =>
                candidate.Id == request.Id
                && candidate.UserId == activation.UserId
                && candidate.RequestType == DigitalServiceRequestType.SubscriptionRequest))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task PortalFundNavAndPortfolioEndpoints_ReturnPublishedFundDataAndPortfolioMetrics()
    {
        using var client = _factory.CreateClient();
        var seed = await SeedPortalInvestorAsync("PORTFOLIO", mfaRequired: true, mfaVerified: true, seedHolding: true);
        await SeedPublishedNavAsync(seed.SchemeId, seed.SchemeClassId, 1_252.50m, 10m);
        await SeedAllocatedSubscriptionAsync(seed.InvestorId, seed.SchemeId, seed.SchemeClassId, 1_000m, 100m, 10m);
        await AuthenticateAsync(client, seed.Email, seed.Password);

        var fundNavResponse = await client.GetAsync("/api/portal/fund-nav");
        fundNavResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fundNav = await ReadResponseAsync<IReadOnlyCollection<PortalFundNavDto>>(fundNavResponse);
        fundNav.Should().ContainSingle(item => item.SchemeId == seed.SchemeId && item.SchemeClassId == seed.SchemeClassId && item.PublishedUnitPrice == 10m);

        var portfolioResponse = await client.GetAsync("/api/portal/portfolio");
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var portfolio = await ReadResponseAsync<PortalPortfolioSummaryDto>(portfolioResponse);
        portfolio.InvestorId.Should().Be(seed.InvestorId);
        portfolio.TotalMarketValue.Should().Be(1252.50m);
        portfolio.TotalNetContribution.Should().Be(1000m);
        portfolio.EstimatedCapitalGain.Should().Be(252.50m);
        portfolio.Positions.Should().ContainSingle(position => position.SchemeId == seed.SchemeId && position.SchemeClassId == seed.SchemeClassId);
    }

    [Fact]
    public async Task PortalKycProfile_ReturnsOfficialKycBankAndNextOfKinDetails()
    {
        using var client = _factory.CreateClient();
        var seed = await SeedPortalInvestorAsync("KYCVIEW", mfaRequired: false, mfaVerified: false, seedHolding: false, seedDetailedKyc: true);
        await AuthenticateAsync(client, seed.Email, seed.Password);

        var response = await client.GetAsync("/api/portal/kyc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await ReadResponseAsync<PortalKycProfileDto>(response);
        profile.InvestorId.Should().Be(seed.InvestorId);
        profile.IdentityNumber.Should().Be("19900101-12345-00001-12");
        profile.AddressLine1.Should().Be("Plot 10, Victory Street");
        profile.AlternatePhoneNumber.Should().Be("+254711111111");
        profile.NextOfKinName.Should().Be("Jane Doe");
        profile.NextOfKinPhoneNumber.Should().Be("+254722222222");
        profile.NextOfKinRelationship.Should().Be("Sibling");
        profile.BankAccounts.Should().ContainSingle(account => account.BankName == "Victory Bank");
        profile.KycRequirements.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task TransferRequest_CreatesWorkflowBackedInternalRequest()
    {
        using var client = _factory.CreateClient();
        var source = await SeedPortalInvestorAsync("TRANSFER-SRC", mfaRequired: false, mfaVerified: false, seedHolding: true, approved: true);
        var target = await SeedPortalInvestorAsync("TRANSFER-TGT", mfaRequired: false, mfaVerified: false, seedHolding: false, approved: true);
        await AuthenticateAsync(client, source.Email, source.Password);

        var response = await client.PostAsJsonAsync("/api/portal/requests/transfer", new CreatePortalTransferRequest(source.SchemeId, source.SchemeClassId, target.InvestorNumber, 25m, "Family transfer request."));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var request = await ReadResponseAsync<DigitalServiceRequestDto>(response);
        request.RequestType.Should().Be(DigitalServiceRequestType.TransferRequest.ToString());
        request.WorkflowId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.DigitalServiceRequests.AnyAsync(candidate => candidate.Id == request.Id && candidate.RequestType == DigitalServiceRequestType.TransferRequest)).Should().BeTrue();
        (await dbContext.WorkflowInstances.AnyAsync(workflow => workflow.Id == request.WorkflowId && workflow.WorkflowType == WorkflowType.PortalDigitalServiceRequest)).Should().BeTrue();
    }

    private async Task<PortalSeed> SeedPortalInvestorAsync(string prefix, bool mfaRequired, bool mfaVerified, bool seedHolding, bool seedTax = false, bool seedDetailedKyc = false, bool approved = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var user = await CreateUserWithRoleDirectlyAsync(UniqueEmail($"portal-{prefix.ToLowerInvariant()}"), $"{prefix} Portal User", "Portal123!", RoleNames.InvestorPortalUser);
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-{prefix}-{Guid.NewGuid():N}"[..30], InvestorType.Individual, $"{prefix} Investor", user.Email, "+254700000000", "test-fixture", now);
        if (seedDetailedKyc || approved)
        {
            investor.AttachIndividualProfile(
                seedDetailedKyc ? "John" : "Approved",
                seedDetailedKyc ? "Doe" : "Investor",
                seedDetailedKyc ? "19900101-12345-00001-12" : $"ID-{Guid.NewGuid():N}"[..20],
                seedDetailedKyc ? new DateOnly(1990, 1, 1) : new DateOnly(1992, 2, 2),
                "Kenyan");
            investor.AddDefaultKycRequirements(["NationalId", "TaxCertificate", "ProofOfAddress"]);
        }
        if (seedTax)
        {
            investor.SetTaxProfile($"TAX-{prefix}", "KE");
        }
        if (seedDetailedKyc)
        {
            investor.SetTaxProfile("A123456789Z", "KE");
            investor.AddContact("Address", "Plot 10, Victory Street", true, "test-fixture", now);
            investor.AddContact("Phone", "+254711111111", false, "test-fixture", now);
            investor.AddContact("NextOfKinName", "Jane Doe", false, "test-fixture", now);
            investor.AddContact("NextOfKinPhoneNumber", "+254722222222", false, "test-fixture", now);
            investor.AddContact("NextOfKinRelationship", "Sibling", false, "test-fixture", now);
            investor.AddDocument("NationalId", "nida.pdf", "application/pdf", 1024, "kyc/nida.pdf", null, null, "test-fixture", now, BusinessDate.From(new DateOnly(2026, 10, 31)));
            investor.AddBankAccount("Victory Bank", "0011223344", "John Doe", "KES", "VFSLKENX", false, "test-fixture", now, "Seed bank account");
        }
        if (approved)
        {
            investor.AddDocument("NationalId", "nid.pdf", "application/pdf", 1024, "kyc/nid.pdf", null, null, "test-fixture", now, BusinessDate.From(new DateOnly(2026, 10, 31)));
            investor.AddDocument("TaxCertificate", "tax.pdf", "application/pdf", 1024, "kyc/tax.pdf", null, null, "test-fixture", now, BusinessDate.From(new DateOnly(2026, 10, 31)));
            investor.AddDocument("ProofOfAddress", "address.pdf", "application/pdf", 1024, "kyc/address.pdf", null, null, "test-fixture", now, BusinessDate.From(new DateOnly(2026, 10, 31)));
            investor.SubmitKyc("test-maker", now);
            investor.Approve("test-approver", now.AddMinutes(1), BusinessDate.From(new DateOnly(2026, 10, 31)), "Approved fixture investor.");
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
        return new PortalSeed(user.Id, user.Email, user.Password, investor.Id, investor.InvestorNumber, scheme.Id, schemeClass.Id, transactionReference);
    }

    private async Task<SelfRegisteredPortalAccount> CreateSelfRegisteredPortalAccountAsync(HttpClient client, string prefix)
    {
        var email = UniqueEmail(prefix);
        const string password = "PortalSelfRegister123!";
        var phoneNumber = $"+2547{Random.Shared.Next(10000000, 99999999)}";

        var createResponse = await client.PostAsJsonAsync("/api/portal/self-registration", new CreatePortalSelfRegistrationRequest($"Self Registered {prefix}", email, phoneNumber, password));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var registration = await ReadResponseAsync<PortalSelfRegistrationInitiatedDto>(createResponse);

        var verifyResponse = await client.PostAsJsonAsync("/api/portal/self-registration/verify-otp", new VerifyPortalSelfRegistrationOtpRequest(registration.RegistrationId, "000000"));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activation = await ReadResponseAsync<PortalSelfRegistrationActivationDto>(verifyResponse);
        return new SelfRegisteredPortalAccount(activation.UserId, email, password, activation.InvestorId);
    }

    private async Task<(Guid SchemeId, Guid ClassId)> CreatePortalSchemeFixtureAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var scheme = Scheme.Create($"SCH-{prefix}-{Guid.NewGuid():N}"[..30], $"{prefix} Scheme", "Unit Trust", "KES", "test-fixture", now);
        var schemeClass = scheme.AddClass("CLS", "Default Class", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(15, 0), 100m, 10m, 0, 0);
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
    }

    private async Task SeedPublishedNavAsync(Guid schemeId, Guid schemeClassId, decimal publishedNav, decimal unitPrice)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var valuationDate = BusinessDate.From(new DateOnly(2026, 10, 31));
        var run = ValuationRun.Create(
            schemeId,
            schemeClassId,
            valuationDate,
            "test-formula-v1",
            DayCountBasis.Actual365,
            4,
            1,
            5m,
            allowsAmortizedCost: false,
            "test-preparer",
            now);
        run.Calculate(new NavFormulaOutput(
            publishedNav,
            0m,
            0m,
            publishedNav,
            0m,
            0m,
            publishedNav,
            0m,
            0m,
            publishedNav,
            100m,
            25.25m,
            0m,
            0m,
            125.25m,
            unitPrice,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m), "test-calculator", now);
        run.Submit("test-submitter", now, null);
        run.Check("test-checker", now.AddMinutes(1), null);
        run.Approve("test-approver", now.AddMinutes(2), null);
        run.Publish("test-publisher", now.AddMinutes(3), 1, null);

        dbContext.ValuationRuns.Add(run);
        dbContext.NavPublications.Add(NavPublication.Create(
            run.Id,
            schemeId,
            schemeClassId,
            valuationDate,
            1,
            publishedNav,
            unitPrice,
            unitPrice,
            run.FormulaVersion,
            "test-publisher",
            now.AddMinutes(3)));
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAllocatedSubscriptionAsync(Guid investorId, Guid schemeId, Guid schemeClassId, decimal amount, decimal units, decimal unitPrice)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var instruction = DealingInstruction.CreateSubscription(
            $"SUB-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            investorId,
            schemeId,
            schemeClassId,
            DealingChannel.InvestorPortal,
            BusinessDate.From(new DateOnly(2026, 10, 31)),
            now,
            DealingInstructionMode.Amount,
            amount,
            units,
            "KES",
            fundsCleared: true,
            approvedNavAvailable: true,
            approvedNavPrice: unitPrice,
            approvedNavDate: BusinessDate.From(new DateOnly(2026, 10, 31)),
            "test-maker",
            now);
        instruction.Submit("test-maker", now);
        instruction.Approve("test-approver", now.AddMinutes(1));
        dbContext.DealingInstructions.Add(instruction);
        await dbContext.SaveChangesAsync();
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

    private sealed record PortalSeed(Guid UserId, string Email, string Password, Guid InvestorId, string InvestorNumber, Guid SchemeId, Guid SchemeClassId, string TransactionReference);
    private sealed record SelfRegisteredPortalAccount(Guid UserId, string Email, string Password, Guid InvestorId);
}
