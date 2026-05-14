using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Cases;
using Cis.Contracts.Identity;
using Cis.Contracts.Reports;
using Cis.Domain.Audit;
using Cis.Domain.Cases;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Cases;

[Collection(IntegrationTestCollection.Name)]
public sealed class CaseManagementModuleTests
{
    private readonly CisApiFactory _factory;

    public CaseManagementModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CasesEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/cases");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CasesEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("cases-portal"), "Cases Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync("/api/cases");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ComplaintCase_CreationEscalationResolutionAndInv16Reporting_WorkEndToEnd()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/cases", new CreateCaseRequest(
            null,
            "General",
            "Delayed redemption advice",
            "Investor reported that redemption advice was not delivered within expected turnaround.",
            ServiceCasePriority.High.ToString(),
            "Email",
            true,
            "Service quality"));
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        var created = await ReadResponseAsync<ServiceCaseDto>(createResponse);
        created.Status.Should().Be(ServiceCaseStatus.Logged.ToString());
        created.OwnerUserId.Should().BeNull();
        created.Complaint.Should().NotBeNull();
        created.SlaTargetAtUtc.Should().BeAfter(created.LoggedAtUtc);
        created.AgingDays.Should().BeGreaterThanOrEqualTo(0);
        created.StatusHistory.Should().ContainSingle(history => history.ToStatus == ServiceCaseStatus.Logged.ToString());

        var assignResponse = await client.PostAsJsonAsync($"/api/cases/{created.Id}/assign", new AssignCaseRequest("case-owner-001"));
        var assignBody = await assignResponse.Content.ReadAsStringAsync();
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, assignBody);
        var assigned = await ReadResponseAsync<ServiceCaseDto>(assignResponse);
        assigned.Status.Should().Be(ServiceCaseStatus.Assigned.ToString());
        assigned.OwnerUserId.Should().Be("case-owner-001");

        var actionResponse = await client.PostAsJsonAsync($"/api/cases/{created.Id}/actions", new AddCaseActionRequest(
            CaseActionType.Investigation.ToString(),
            "Confirmed delivery exception with operations team.",
            "documents/cases/redemption-advice-evidence.pdf"));
        actionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var actioned = await ReadResponseAsync<ServiceCaseDto>(actionResponse);
        actioned.Actions.Should().Contain(action => action.ActionType == CaseActionType.Investigation.ToString());

        var escalationResponse = await client.PostAsJsonAsync($"/api/cases/{created.Id}/escalate", new EscalateCaseRequest(
            "SLA breach risk requires compliance oversight.",
            RoleNames.ComplianceRiskOfficer,
            null));
        escalationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var escalated = await ReadResponseAsync<ServiceCaseDto>(escalationResponse);
        escalated.Status.Should().Be(ServiceCaseStatus.Escalated.ToString());
        escalated.Escalations.Should().ContainSingle(escalation => escalation.Status == CaseEscalationStatus.Open.ToString());

        var resolveResponse = await client.PostAsJsonAsync($"/api/cases/{created.Id}/resolve", new ResolveCaseRequest(
            "Redemption advice reissued and investor acknowledged receipt.",
            "documents/cases/resolution-confirmation.pdf"));
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolved = await ReadResponseAsync<ServiceCaseDto>(resolveResponse);
        resolved.Status.Should().Be(ServiceCaseStatus.Resolved.ToString());
        resolved.ResolutionEvidenceReference.Should().Be("documents/cases/resolution-confirmation.pdf");
        resolved.Escalations.Should().OnlyContain(escalation => escalation.Status == CaseEscalationStatus.Resolved.ToString());
        resolved.Complaint!.TurnaroundDays.Should().BeGreaterThanOrEqualTo(0);

        var dashboardResponse = await client.GetAsync("/api/cases/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await ReadResponseAsync<CaseDashboardDto>(dashboardResponse);
        dashboard.TotalCases.Should().BeGreaterThan(0);
        dashboard.ResolvedCases.Should().BeGreaterThan(0);
        dashboard.AverageTurnaroundDays.Should().BeGreaterThanOrEqualTo(0m);

        var reportResponse = await client.GetAsync("/api/reports/INV-16/query?fromDate=2026-01-01&toDate=2026-12-31");
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await ReadResponseAsync<ReportQueryResultDto>(reportResponse);
        var row = report.Rows.Single();
        row.SourceEntity.Should().Be("Complaints");
        row.SourceRecordCount.Should().BeGreaterThan(0);
        row.AmountTotal.Should().BeGreaterThanOrEqualTo(0m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(log => log.Module == "Cases" && log.EntityId == created.Id.ToString())
            .Select(log => new { log.Action, log.EventType })
            .ToListAsync();
        actions.Select(action => action.Action).Should().Contain(["CaseCreated", "CaseAssigned", "CaseActionAdded", "CaseEscalated", "CaseResolved"]);
        actions.Should().Contain(action => action.Action == "CaseCreated" && action.EventType == AuditEventType.Created);
        actions.Should().Contain(action => action.Action == "CaseEscalated" && action.EventType == AuditEventType.Updated);
    }

    [Fact]
    public async Task CaseValidation_RejectsInvalidPriorityAndResolutionWithoutEvidence()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var invalidCreate = await client.PostAsJsonAsync("/api/cases", new CreateCaseRequest(
            null,
            "General",
            "Invalid priority",
            "This case uses an unsupported priority.",
            "Extreme",
            "Branch",
            false,
            null));
        invalidCreate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalidCreate.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var created = await CreateAssignedCaseAsync(client);
        var invalidResolution = await client.PostAsJsonAsync($"/api/cases/{created.Id}/resolve", new ResolveCaseRequest(
            "Resolved but missing evidence.",
            string.Empty));

        invalidResolution.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        invalidResolution.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<ServiceCaseDto> CreateAssignedCaseAsync(HttpClient client)
    {
        var createResponse = await client.PostAsJsonAsync("/api/cases", new CreateCaseRequest(
            null,
            "General",
            "Statement complaint",
            "Investor did not receive statement.",
            ServiceCasePriority.Medium.ToString(),
            "Phone",
            false,
            null));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadResponseAsync<ServiceCaseDto>(createResponse);
        var assignResponse = await client.PostAsJsonAsync($"/api/cases/{created.Id}/assign", new AssignCaseRequest("case-owner-002"));
        var assignBody = await assignResponse.Content.ReadAsStringAsync();
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK, assignBody);
        return await ReadResponseAsync<ServiceCaseDto>(assignResponse);
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
