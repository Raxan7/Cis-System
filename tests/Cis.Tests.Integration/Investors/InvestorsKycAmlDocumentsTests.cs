using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Investors;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Investors;

[Collection(IntegrationTestCollection.Name)]
public sealed class InvestorsKycAmlDocumentsTests
{
    private readonly CisApiFactory _factory;

    public InvestorsKycAmlDocumentsTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedInvestorsEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/investors");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedInvestorsEndpoint_WithAuthenticatedUserMissingPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var createdUser = await CreateUserAsync(client, UniqueEmail("investor-no-permission"), "Investor No Permission", "NoPermission123!");

        var token = await LoginAsync(client, createdUser.Email, "NoPermission123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync("/api/investors");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateInvestor_WhenIndividualRequiredFieldsAreMissing_ReturnsValidationProblemDetails()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var request = IndividualRequest(UniqueEmail("missing-id"), identityNumber: "");

        var response = await client.PostAsJsonAsync("/api/investors", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("identityNumber");
    }

    [Fact]
    public async Task CreateInvestor_WhenIdentifiersAlreadyExist_ReturnsDuplicateWarningsBeforeActivation()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var identityNumber = $"ID-{Guid.NewGuid():N}"[..20];
        _ = await CreateInvestorAsync(client, IndividualRequest(UniqueEmail("duplicate-source"), identityNumber));

        var response = await client.PostAsJsonAsync("/api/investors", IndividualRequest(UniqueEmail("duplicate-target"), identityNumber));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var duplicate = await ReadResponseAsync<InvestorDto>(response);

        duplicate.Status.Should().Be("Draft");
        duplicate.DuplicateDetectionResults.Should().Contain(result =>
            result.MatchType == "IdentityNumber"
            && result.MatchedValue == identityNumber.ToUpperInvariant());
    }

    [Fact]
    public async Task InvestorApproval_WithCompleteKycAndClearAml_BySeparateApprover_ApprovesAndWritesAudit()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorAsync(client, IndividualRequest(UniqueEmail("happy-investor"), UniqueIdentity()));
        investor = await AddRequiredDocumentsAsync(client, investor, new DateOnly(2030, 12, 31));

        var amlResponse = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/aml-screening",
            new StartAmlScreeningRequest("ManualStub", null, []));
        amlResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResponse = await client.PostAsync($"/api/investors/{investor.Id}/submit-kyc", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("investor-approver"), "Investor Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/approve",
            new InvestorWorkflowDecisionRequest("Approved complete investor KYC."));
        var approveBody = await approveResponse.Content.ReadAsStringAsync();
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK, approveBody);
        var approved = await ReadResponseAsync<InvestorDto>(approveResponse);

        approved.Status.Should().Be("Approved");
        approved.RiskClassifications.Should().ContainSingle(classification => classification.Status == "Approved");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.EntityId == investor.Id.ToString())
            .Select(auditLog => auditLog.Action)
            .ToListAsync();
        actions.Should().Contain("InvestorCreated");
        actions.Should().Contain("KycDocumentUploaded");
        actions.Should().Contain("AmlScreeningPerformed");
        actions.Should().Contain("KycSubmitted");
        actions.Should().Contain("InvestorApproved");
    }

    [Fact]
    public async Task InvestorApproval_WithExpiredKycDocument_ReturnsValidationProblemDetailsAndExpiredDocumentQueryShowsIt()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorAsync(client, IndividualRequest(UniqueEmail("expired-doc"), UniqueIdentity()));
        investor = await AddRequiredDocumentsAsync(client, investor, new DateOnly(2020, 1, 1), expiredDocumentType: "NationalId");

        var submitResponse = await client.PostAsync($"/api/investors/{investor.Id}/submit-kyc", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("expired-doc-approver"), "Expired Doc Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/approve",
            new InvestorWorkflowDecisionRequest("Try approve expired KYC."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        approveResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await approveResponse.Content.ReadAsStringAsync();
        body.Should().Contain("required KYC documents are missing or expired");

        var expiredResponse = await client.GetAsync("/api/kyc/expired-documents");
        expiredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var expiredDocuments = await ReadResponseAsync<IReadOnlyCollection<KycDocumentDto>>(expiredResponse);
        expiredDocuments.Should().Contain(document => document.InvestorId == investor.Id && document.DocumentType == "NationalId");
    }

    [Fact]
    public async Task InvestorApproval_WithUnresolvedHighRiskAmlHit_ReturnsValidationProblemDetailsAndAmlException()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorAsync(client, IndividualRequest(UniqueEmail("aml-hit"), UniqueIdentity()));
        investor = await AddRequiredDocumentsAsync(client, investor, new DateOnly(2030, 12, 31));

        var amlResponse = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/aml-screening",
            new StartAmlScreeningRequest(
                "ManualStub",
                null,
                [new ManualAmlHitRequest("Sanctions", "High Risk Person", "High", "Manual high-risk match.")]));
        amlResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResponse = await client.PostAsync($"/api/investors/{investor.Id}/submit-kyc", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("aml-approver"), "AML Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/approve",
            new InvestorWorkflowDecisionRequest("Try approve high-risk AML."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await approveResponse.Content.ReadAsStringAsync();
        body.Should().Contain("unresolved AML high-risk hits");

        var exceptionsResponse = await client.GetAsync("/api/aml/exceptions");
        exceptionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var exceptions = await ReadResponseAsync<IReadOnlyCollection<AmlExceptionDto>>(exceptionsResponse);
        exceptions.Should().Contain(exception => exception.InvestorId == investor.Id && exception.RiskLevel == "High");
    }

    [Fact]
    public async Task BankAccountChange_WritesAuditLogAndVisibleChangeLog()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorAsync(client, IndividualRequest(UniqueEmail("bank-change"), UniqueIdentity()));

        var response = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/bank-accounts",
            new AddInvestorBankAccountRequest("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", investor.DisplayName, "KES", "VICBKENA", true, "High-risk bank detail change."));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadResponseAsync<InvestorDto>(response);

        updated.BankAccounts.Should().ContainSingle(account => account.HighRiskFlag);
        updated.ChangeLogs.Should().Contain(log => log.ChangeType == "BankAccountChanged" && log.HighRiskFlag);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var auditExists = await dbContext.AuditLogs.AnyAsync(auditLog =>
            auditLog.Module == "Investors"
            && auditLog.Action == "InvestorBankAccountChanged"
            && auditLog.EntityId == investor.Id.ToString());
        auditExists.Should().BeTrue();
    }

    private async Task<InvestorDto> CreateInvestorAsync(HttpClient client, CreateInvestorRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/investors", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return await ReadResponseAsync<InvestorDto>(response);
    }

    private async Task<InvestorDto> AddRequiredDocumentsAsync(HttpClient client, InvestorDto investor, DateOnly defaultExpiry, string? expiredDocumentType = null)
    {
        foreach (var documentType in new[] { "NationalId", "TaxCertificate", "ProofOfAddress" })
        {
            var expiry = documentType == expiredDocumentType ? new DateOnly(2020, 1, 1) : defaultExpiry;
            var response = await client.PostAsJsonAsync(
                $"/api/investors/{investor.Id}/documents",
                new AddKycDocumentRequest(
                    documentType,
                    $"{documentType}-{Guid.NewGuid():N}.pdf",
                    "application/pdf",
                    2048,
                    $"documents/{investor.Id}/{documentType}-{Guid.NewGuid():N}.pdf",
                    new DateOnly(2025, 1, 1),
                    expiry));
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, body);
            investor = await ReadResponseAsync<InvestorDto>(response);
        }

        return investor;
    }

    private static CreateInvestorRequest IndividualRequest(string email, string identityNumber)
    {
        return new CreateInvestorRequest(
            "Individual",
            $"Investor {Guid.NewGuid():N}"[..20],
            email,
            $"+2547{Random.Shared.Next(10000000, 99999999)}",
            "Low",
            "Standard onboarding risk.",
            identityNumber,
            "Jane",
            "Investor",
            new DateOnly(1990, 1, 1),
            "Kenyan",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            $"TAX-{Guid.NewGuid():N}"[..20],
            "Kenya",
            "1 Victory Way",
            "Standard",
            "Single Signatory",
            new DateOnly(2026, 1, 1),
            null);
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

    private static string UniqueIdentity()
    {
        return $"ID-{Guid.NewGuid():N}"[..20];
    }
}
