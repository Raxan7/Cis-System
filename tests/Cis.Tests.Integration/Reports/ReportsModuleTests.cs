using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Reports;
using Cis.Domain.Audit;
using Cis.Domain.Identity;
using Cis.Domain.Reports;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Reports;

[Collection(IntegrationTestCollection.Name)]
public sealed class ReportsModuleTests
{
    private readonly CisApiFactory _factory;

    public ReportsModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReportsEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/reports/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ReportsEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var user = await CreateUserWithRoleDirectlyAsync(UniqueEmail("reports-portal"), "Reports Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, user.Email, user.Password);

        var response = await client.GetAsync("/api/reports/definitions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DefinitionsAndSchedules_ExposeRegistryAndPersistScheduleWithAudit()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var definitionsResponse = await client.GetAsync("/api/reports/definitions");
        definitionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var definitions = await ReadResponseAsync<IReadOnlyCollection<ReportDefinitionDto>>(definitionsResponse);
        definitions.Should().Contain(definition => definition.Code == "NAV-DAILY" && definition.Owners.Count > 0);
        definitions.Should().Contain(definition => definition.Code == "REG-PACK" && definition.RequiredPermission == Permissions.Reports.RegulatorPack);
        definitions.Select(definition => definition.Code).Should().Contain(ReportCatalogueCodes.All);

        var scheduleResponse = await client.PostAsJsonAsync("/api/reports/schedules", new CreateReportScheduleRequest(
            "NAV-DAILY",
            ReportFrequency.Daily.ToString(),
            "0 18 * * MON-FRI",
            DateTime.UtcNow.AddHours(4)));
        var body = await scheduleResponse.Content.ReadAsStringAsync();
        scheduleResponse.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var schedule = await ReadResponseAsync<ReportScheduleDto>(scheduleResponse);
        schedule.ReportCode.Should().Be("NAV-DAILY");
        schedule.Status.Should().Be(ReportScheduleStatus.Active.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.ReportSchedules.AnyAsync(item => item.Id == schedule.Id)).Should().BeTrue();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Reports" && log.Action == "ReportScheduleCreated")).Should().BeTrue();
    }

    [Fact]
    public async Task CompleteReportCatalogue_HasQueryHandlersAndCsvExportsForEveryReportCode()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var definitionsResponse = await client.GetAsync("/api/reports/definitions");
        definitionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var definitions = await ReadResponseAsync<IReadOnlyCollection<ReportDefinitionDto>>(definitionsResponse);
        var definitionCodes = definitions.Select(definition => definition.Code).ToArray();
        definitionCodes.Should().Contain(ReportCatalogueCodes.All);

        foreach (var code in ReportCatalogueCodes.All)
        {
            var queryResponse = await client.GetAsync($"/api/reports/{code}/query?fromDate=2026-01-01&toDate=2026-12-31");
            var queryBody = await queryResponse.Content.ReadAsStringAsync();
            queryResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"{code}: {queryBody}");
            var result = await ReadResponseAsync<ReportQueryResultDto>(queryResponse);
            result.Code.Should().Be(code);
            result.Rows.Should().ContainSingle();
            result.Rows.Single().Code.Should().Be(code);
            result.Rows.Single().FromDate.Should().Be(new DateOnly(2026, 1, 1));
            result.Rows.Single().ToDate.Should().Be(new DateOnly(2026, 12, 31));

            var csvResponse = await client.GetAsync($"/api/reports/{code}/export/csv?fromDate=2026-01-01&toDate=2026-12-31");
            var csvBody = await csvResponse.Content.ReadAsStringAsync();
            csvResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"{code}: {csvBody}");
            var csv = await ReadResponseAsync<ReportCsvExportDto>(csvResponse);
            csv.Code.Should().Be(code);
            csv.ContentType.Should().Be("text/csv");
            csv.Content.Should().StartWith("Code,Name,Category,FromDate,ToDate,SourceEntity,SourceRecordCount,ExceptionCount,AmountTotal,GeneratedAtUtc");
            csv.Content.Should().Contain(code);
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var investorCount = await dbContext.Investors.CountAsync();
        var investorReportResponse = await client.GetAsync("/api/reports/INV-01/query");
        var investorReport = await ReadResponseAsync<ReportQueryResultDto>(investorReportResponse);
        investorReport.Rows.Single().SourceRecordCount.Should().Be(investorCount);

        var schemeCount = await dbContext.Schemes.CountAsync();
        var schemeReportResponse = await client.GetAsync("/api/reports/SCH-01/query");
        var schemeReport = await ReadResponseAsync<ReportQueryResultDto>(schemeReportResponse);
        schemeReport.Rows.Single().SourceRecordCount.Should().Be(schemeCount);

        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Reports" && log.Action == "ReportCsvExported")).Should().BeTrue();
    }

    [Fact]
    public async Task RegulatorReportQuery_RequiresRegulatorPackPermission()
    {
        using var client = _factory.CreateClient();
        var boardUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("reports-board"), "Reports Board", "Board123!", RoleNames.BoardUser);
        await AuthenticateAsync(client, boardUser.Email, boardUser.Password);

        var definitionsResponse = await client.GetAsync("/api/reports/definitions");
        definitionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var definitions = await ReadResponseAsync<IReadOnlyCollection<ReportDefinitionDto>>(definitionsResponse);
        definitions.Should().NotContain(definition => definition.Code == "REG-01");

        var queryResponse = await client.GetAsync("/api/reports/REG-01/query");
        queryResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReportRun_RequiresApprovalBeforePublication_ArchivesImmutableVersion_AndLogsDownload()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var invalidRun = await client.PostAsJsonAsync("/api/reports/NAV-DAILY/run", new RunReportRequest(
            new DateOnly(2026, 10, 31),
            [],
            []));
        invalidRun.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var runResponse = await client.PostAsJsonAsync("/api/reports/NAV-DAILY/run", RunRequest());
        var runBody = await runResponse.Content.ReadAsStringAsync();
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created, runBody);
        var run = await ReadResponseAsync<ReportRunDto>(runResponse);
        run.Status.Should().Be(ReportRunStatus.Generated.ToString());
        run.Parameters.Should().Contain(parameter => parameter.Name == "valuationDate" && parameter.Value == "2026-10-31");
        run.SourceDataTimestampUtc.Kind.Should().Be(DateTimeKind.Utc);
        run.Outputs.Should().Contain(output => output.Format == ReportOutputFormat.PDF.ToString());

        var prematurePublish = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/publish", new ReportActionRequest("Try publish early."));
        prematurePublish.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var selfApprove = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/approve", new ReportActionRequest("Self approval attempt."));
        selfApprove.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var approver = await CreateUserWithRoleDirectlyAsync(UniqueEmail("reports-approver"), "Reports Approver", "Approver123!", RoleNames.SystemAdmin);
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/approve", new ReportActionRequest("Report checked and approved."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await ReadResponseAsync<ReportRunDto>(approveResponse);
        approved.Status.Should().Be(ReportRunStatus.Approved.ToString());
        approved.Approvals.Should().ContainSingle();

        var publishResponse = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/publish", new ReportActionRequest("Official publication."));
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await ReadResponseAsync<ReportRunDto>(publishResponse);
        published.Status.Should().Be(ReportRunStatus.Published.ToString());
        published.Distributions.Should().Contain(distribution => distribution.Channel == ReportDeliveryChannel.SecurePortal.ToString());

        var downloadResponse = await client.GetAsync($"/api/reports/runs/{run.Id}/download");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var download = await ReadResponseAsync<ReportDownloadDto>(downloadResponse);
        download.ReportRunId.Should().Be(run.Id);
        download.StorageReference.Should().Contain("reports/NAV-DAILY");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var archive = await dbContext.ReportVersionArchives.SingleAsync(item => item.ReportRunId == run.Id);
        archive.PayloadHash.Should().NotBeNullOrWhiteSpace();
        dbContext.ReportVersionArchives.Remove(archive);
        var immutability = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
        immutability.Message.Should().Contain("immutable");

        using var auditScope = _factory.Services.CreateScope();
        var auditDb = auditScope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await auditDb.AuditLogs.Where(log => log.Module == "Reports").Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["ReportRunGenerated", "ReportRunApproved", "ReportRunPublished", "ReportRunDownloaded"]);
    }

    [Fact]
    public async Task ReportBundles_RequirePublishedRunsBeforeBundlePublication()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var first = await CreateApprovedPublishedRunAsync(client);
        var secondResponse = await client.PostAsJsonAsync("/api/reports/NAV-DAILY/run", RunRequest());
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var unpublished = await ReadResponseAsync<ReportRunDto>(secondResponse);

        var blockedBundleResponse = await client.PostAsJsonAsync("/api/reports/bundles", new CreateReportBundleRequest(
            $"BND-{Guid.NewGuid():N}"[..24],
            "Month-end blocked bundle",
            new DateOnly(2026, 10, 31),
            [first.Id, unpublished.Id]));
        blockedBundleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var blockedBundle = await ReadResponseAsync<ReportBundleDto>(blockedBundleResponse);
        var blockedPublish = await client.PostAsync($"/api/reports/bundles/{blockedBundle.Id}/publish", null);
        blockedPublish.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var bundleResponse = await client.PostAsJsonAsync("/api/reports/bundles", new CreateReportBundleRequest(
            $"BND-{Guid.NewGuid():N}"[..24],
            "Month-end official bundle",
            new DateOnly(2026, 10, 31),
            [first.Id]));
        bundleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var bundle = await ReadResponseAsync<ReportBundleDto>(bundleResponse);
        bundle.Status.Should().Be(ReportBundleStatus.Draft.ToString());

        var publishResponse = await client.PostAsync($"/api/reports/bundles/{bundle.Id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await ReadResponseAsync<ReportBundleDto>(publishResponse);
        published.Status.Should().Be(ReportBundleStatus.Published.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Reports" && log.Action == "ReportBundlePublished")).Should().BeTrue();
    }

    private async Task<ReportRunDto> CreateApprovedPublishedRunAsync(HttpClient client)
    {
        var runResponse = await client.PostAsJsonAsync("/api/reports/NAV-DAILY/run", RunRequest());
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<ReportRunDto>(runResponse);
        var approver = await CreateUserWithRoleDirectlyAsync(UniqueEmail("reports-publisher"), "Reports Publisher", "Publisher123!", RoleNames.SystemAdmin);
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/approve", new ReportActionRequest("Approved."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishResponse = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/publish", new ReportActionRequest("Published."));
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<ReportRunDto>(publishResponse);
    }

    private static RunReportRequest RunRequest()
    {
        return new RunReportRequest(
            new DateOnly(2026, 10, 31),
            [
                new ReportParameterRequest("valuationDate", "2026-10-31"),
                new ReportParameterRequest("schemeCode", "ALL")
            ],
            [
                new ReportOutputRequest(ReportOutputFormat.PDF.ToString()),
                new ReportOutputRequest(ReportOutputFormat.Excel.ToString())
            ]);
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
