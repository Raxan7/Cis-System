using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Contracts;
using Cis.Contracts.Cash;
using Cis.Contracts.Identity;
using Cis.Contracts.Investors;
using Cis.Contracts.Reports;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Performance;

[Collection(IntegrationTestCollection.Name)]
public sealed class PerformanceReadinessTests
{
    private readonly CisApiFactory _factory;

    public PerformanceReadinessTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InvestorSearch_ReturnsPaginatedResultWithinBaselineThreshold()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        await SeedInvestorsAsync(600);

        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/investors?pageNumber=2&pageSize=25&sortBy=DisplayName&search=Performance Investor");
        stopwatch.Stop();

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyCollection<InvestorDto>>>();
        payload.Should().NotBeNull();
        payload!.Data.Should().HaveCount(25);
        payload.Meta.Pagination.Should().NotBeNull();
        payload.Meta.Pagination!.PageNumber.Should().Be(2);
        payload.Meta.Pagination.PageSize.Should().Be(25);
        payload.Meta.Pagination.TotalCount.Should().BeGreaterThanOrEqualTo(600);
        payload.Data.All(investor => investor.DisplayName.Contains("Performance Investor", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task BankStatementImport_WithThousandsOfLines_IsIdempotent_AndRunsThroughBackgroundQueue()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var bankAccountId = await SeedSchemeBankAccountAsync("PERF-CASH");
        var beforeJobs = await GetJobsDashboardAsync(client);
        var csv = BuildBankStatementCsv(1500);
        var request = new ImportBankStatementRequest(bankAccountId, new DateOnly(2026, 12, 31), 2, "perf-bank-import.csv", csv);

        var first = await PostAsJsonWithIdempotencyAsync(client, "/api/cash/bank-statements/import", request, "perf-cash-import-001");
        var firstBody = await first.Content.ReadAsStringAsync();
        first.StatusCode.Should().Be(HttpStatusCode.Created, firstBody);
        var firstPayload = await first.Content.ReadFromJsonAsync<ApiResponse<BankStatementImportDto>>();
        firstPayload.Should().NotBeNull();
        firstPayload!.Data.LineCount.Should().Be(1500);
        firstPayload.Data.SuspenseLineCount.Should().Be(1500);

        var replay = await PostAsJsonWithIdempotencyAsync(client, "/api/cash/bank-statements/import", request, "perf-cash-import-001");
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayPayload = await replay.Content.ReadFromJsonAsync<ApiResponse<BankStatementImportDto>>();
        replayPayload.Should().NotBeNull();
        replayPayload!.Data.Id.Should().Be(firstPayload.Data.Id);

        var afterJobs = await GetJobsDashboardAsync(client);
        afterJobs.CompletedCount.Should().BeGreaterThan(beforeJobs.CompletedCount);
    }

    [Fact]
    public async Task HeavyReportRun_CompletesViaBackgroundQueue_AndStoresRun()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var beforeJobs = await GetJobsDashboardAsync(client);

        var response = await client.PostAsJsonAsync("/api/reports/REG-01/run", new RunReportRequest(
            new DateOnly(2026, 12, 31),
            [new ReportParameterRequest("period", "2026-12")],
            [
                new ReportOutputRequest("CSV"),
                new ReportOutputRequest("HTML")
            ]));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<ReportRunDto>>();
        payload.Should().NotBeNull();
        payload!.Data.Status.Should().Be("Generated");
        payload.Data.Outputs.Should().HaveCount(2);

        var afterJobs = await GetJobsDashboardAsync(client);
        afterJobs.CompletedCount.Should().BeGreaterThan(beforeJobs.CompletedCount);
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
        var token = await response.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>();
        token.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Data.AccessToken);
        return token.Data;
    }

    private async Task SeedInvestorsAsync(int count)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;

        var investors = new List<Investor>(count);
        for (var index = 0; index < count; index++)
        {
            var investor = Investor.Create(
                $"PERF-INV-{Guid.NewGuid():N}"[..30],
                InvestorType.Individual,
                $"Performance Investor {index:D4}",
                $"perf-investor-{Guid.NewGuid():N}@victoryfs.local",
                $"+2547{Random.Shared.Next(10000000, 99999999)}",
                "performance-fixture",
                now);
            investor.AttachIndividualProfile($"Perf{index}", "Investor", $"ID-{Guid.NewGuid():N}"[..20], new DateOnly(1990, 1, 1), "Kenyan");
            investors.Add(investor);
        }

        dbContext.Investors.AddRange(investors);
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedSchemeBankAccountAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var scheme = Scheme.Create($"{prefix}-{Guid.NewGuid():N}"[..30], $"{prefix} Scheme", "Unit Trust", "KES", "performance-fixture", now);
        scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddBankAccount("Victory Bank", $"0988{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return await dbContext.SchemeBankAccounts
            .Where(account => account.SchemeId == scheme.Id)
            .Select(account => account.Id)
            .SingleAsync();
    }

    private static string BuildBankStatementCsv(int rows)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("LineNumber,TransactionDate,Reference,Description,Amount,Direction,InvestorId,SchemeId,SchemeClassId");
        for (var index = 1; index <= rows; index++)
        {
            builder.Append(index)
                .Append(",2026-12-31,PERF-REF-")
                .Append(index.ToString("D5"))
                .Append(",Performance import row,")
                .Append(1000m + index)
                .Append(",Credit,,,")
                .AppendLine();
        }

        return builder.ToString();
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

    private static async Task<BackgroundJobsDashboardDto> GetJobsDashboardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/jobs");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<BackgroundJobsDashboardDto>>();
        payload.Should().NotBeNull();
        return payload!.Data;
    }

    private sealed record BackgroundJobsDashboardDto(
        string Provider,
        bool DashboardEnabled,
        int QueuedCount,
        int ActiveCount,
        long CompletedCount,
        long FailedCount,
        DateTime? LastCompletedAtUtc,
        DateTime? LastFailedAtUtc,
        DateTime RetrievedAtUtc);
}
