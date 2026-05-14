using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Integrations;
using Cis.Contracts.Reports;
using Cis.Application.Common.Security;
using Cis.Domain.CustodyReconciliation;
using Cis.Domain.Identity;
using Cis.Domain.Integrations;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Integrations;

[Collection(IntegrationTestCollection.Name)]
public sealed class IntegrationsModuleTests
{
    private readonly CisApiFactory _factory;

    public IntegrationsModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedIntegrationEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/integrations/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedIntegrationEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("integrations-portal"), "Integrations Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync("/api/integrations/messages");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BankStatementUpload_IsIdempotent_StoresPayload_AndAuditsSuccess()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var seed = await SeedSchemeWithCustodianAsync("INT-BANK");
        var request = new UploadBankStatementIntegrationRequest(
            seed.BankAccountId,
            new DateOnly(2026, 11, 30),
            "bank-statement.csv",
            "date,reference,amount\n2026-11-30,DEP-001,1500.00");

        var first = await PostAsJsonWithIdempotencyAsync(client, "/api/integrations/bank-statements/upload", request, "int-bank-001");
        var firstBody = await first.Content.ReadAsStringAsync();
        first.StatusCode.Should().Be(HttpStatusCode.Created, firstBody);
        var firstResult = await ReadResponseAsync<IntegrationResultDto>(first);
        firstResult.Message.Status.Should().Be("Processed");
        firstResult.IngestionRun!.RecordCount.Should().Be(1);
        firstResult.StoredDocumentReference.Should().NotBeNullOrWhiteSpace();
        File.Exists(firstResult.StoredDocumentReference).Should().BeTrue();

        var replay = await PostAsJsonWithIdempotencyAsync(client, "/api/integrations/bank-statements/upload", request, "int-bank-001");
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayResult = await ReadResponseAsync<IntegrationResultDto>(replay);
        replayResult.Message.Id.Should().Be(firstResult.Message.Id);

        var conflict = await PostAsJsonWithIdempotencyAsync(client, "/api/integrations/bank-statements/upload", request with { CsvContent = "date,reference,amount\n2026-11-30,DEP-002,2000.00" }, "int-bank-001");
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.IntegrationMessages.CountAsync(message => message.EndpointCode == "BANK-STATEMENT-UPLOAD")).Should().Be(1);
        (await dbContext.IntegrationIdempotencyKeys.CountAsync(key => key.EndpointCode == "BANK-STATEMENT-UPLOAD" && key.Key == "int-bank-001")).Should().Be(1);
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Integrations" && log.Action == "IntegrationInboundProcessed")).Should().BeTrue();
    }

    [Fact]
    public async Task FailedInboundIntegration_LogsErrorAndReturnsProblemDetails()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var seed = await SeedSchemeWithCustodianAsync("INT-FAIL");
        var request = new UploadBankStatementIntegrationRequest(seed.BankAccountId, new DateOnly(2026, 11, 30), "bad.csv", "date,reference,amount\nFAIL");

        var response = await PostAsJsonWithIdempotencyAsync(client, "/api/integrations/bank-statements/upload", request, "int-bank-fail-001");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.IntegrationErrors.AnyAsync(error => error.IntegrationType == IntegrationType.Banking && error.ErrorCode == "INGESTION_FAILED")).Should().BeTrue();
    }

    [Fact]
    public async Task PricingUpload_CreatesPriceSourceHierarchy_AndErpExportProducesSummaryCsv()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var seed = await SeedSchemeWithCustodianAsync("INT-PRICE");
        var pricing = new PricingUploadRequest(
            seed.SchemeId,
            seed.SchemeClassId,
            "TreasuryBill",
            "NSE",
            "ManualUpload",
            true,
            2,
            5m,
            new DateOnly(2026, 11, 30),
            "prices.csv",
            "instrument,price\nTBILL-91,99.125");

        var pricingResponse = await PostAsJsonWithIdempotencyAsync(client, "/api/integrations/pricing/upload", pricing, "pricing-001");
        var pricingBody = await pricingResponse.Content.ReadAsStringAsync();
        pricingResponse.StatusCode.Should().Be(HttpStatusCode.Created, pricingBody);
        var pricingResult = await ReadResponseAsync<IntegrationResultDto>(pricingResponse);
        pricingResult.IngestionRun!.AdapterName.Should().Be("FileBasedImportAdapter");

        var erpResponse = await client.PostAsJsonAsync("/api/integrations/erp/export", new ErpExportRequest(new DateOnly(2026, 11, 30), "ERP-NOV-2026"));
        erpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var erp = await ReadResponseAsync<IntegrationResultDto>(erpResponse);
        erp.OutputContent.Should().StartWith("ExportCode,BusinessDate,JournalCount,LedgerEntryCount,GeneratedAtUtc");
        erp.StoredDocumentReference.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var hierarchy = await dbContext.PriceSourceHierarchies.SingleAsync(source => source.SchemeId == seed.SchemeId && source.SchemeClassId == seed.SchemeClassId && source.InstrumentType == "TreasuryBill");
        hierarchy.PrimarySource.Should().Be("NSE");
        (await dbContext.IntegrationMessages.AnyAsync(message => message.EndpointCode == "ERP-EXPORT")).Should().BeTrue();
    }

    [Fact]
    public async Task RetryableNotificationFailure_CreatesDeliveryAttemptErrorAndDgt03SourceData()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/integrations/notifications/test-email", new TestEmailRequest(
            "ops@victoryfs.local",
            "Integration failure test",
            "Simulate retryable email failure.",
            SimulateFailure: true,
            RetryableFailure: true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseAsync<IntegrationResultDto>(response);
        result.Message.Status.Should().Be("PendingRetry");
        result.DeliveryAttempt!.Retryable.Should().BeTrue();
        result.Error!.Retryable.Should().BeTrue();

        var reportResponse = await client.GetAsync("/api/reports/DGT-03/query");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await ReadResponseAsync<ReportQueryResultDto>(reportResponse);
        report.Rows.Single().SourceEntity.Should().Be("IntegrationErrors");
        report.Rows.Single().SourceRecordCount.Should().BeGreaterThan(0);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.IntegrationDeliveryAttempts.AnyAsync(attempt => attempt.Retryable)).Should().BeTrue();
        (await dbContext.IntegrationErrors.AnyAsync(error => error.IntegrationType == IntegrationType.Email && error.Retryable)).Should().BeTrue();
    }

    private async Task<(Guid SchemeId, Guid SchemeClassId, Guid BankAccountId, Guid CustodianId)> SeedSchemeWithCustodianAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var scheme = Scheme.Create($"{prefix}-{Guid.NewGuid():N}"[..30], $"{prefix} Scheme", "Unit Trust", "KES", "test-fixture", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddBankAccount("Victory Bank", $"0999{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        var bankAccount = await dbContext.SchemeBankAccounts.SingleAsync(account => account.SchemeId == scheme.Id);

        var custodian = Custodian.Create($"{prefix[..Math.Min(prefix.Length, 8)]}{Random.Shared.Next(1000, 9999)}", $"{prefix} Custodian", null, "test-fixture", now);
        custodian.AddAccount(scheme.Id, schemeClass.Id, "CUST-CASH-001", "Custody account", "KES");
        dbContext.Custodians.Add(custodian);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id, bankAccount.Id, custodian.Id);
    }

    private async Task AuthenticateAsBootstrapAdminAsync(HttpClient client)
    {
        await AuthenticateAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
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
