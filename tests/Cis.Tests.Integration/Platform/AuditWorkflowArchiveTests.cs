using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Archive;
using Cis.Contracts.Identity;
using Cis.Contracts.Workflows;
using Cis.Domain.Archive;
using Cis.Domain.Audit;
using Cis.Domain.Identity;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Platform;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuditWorkflowArchiveTests
{
    private readonly CisApiFactory _factory;

    public AuditWorkflowArchiveTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/audit-logs")]
    [InlineData("/api/workflows/pending")]
    [InlineData("/api/archive")]
    public async Task ProtectedPlatformEndpoints_WithoutToken_ReturnUnauthorized(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Workflow_CanBeSubmittedCheckedAndApprovedBySeparateUsers()
    {
        using var client = _factory.CreateClient();
        var maker = await AuthenticateAsBootstrapAdminAsync(client);
        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("workflow-checker"), "Workflow Checker", "Checker123!");
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("workflow-approver"), "Workflow Approver", "Approver123!");
        var workflowId = await CreateWorkflowDirectlyAsync(maker.User.Id, "nav-approval-success");

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/submit",
            new WorkflowActionRequest("NAV prepared for checker review."));
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, submitBody);
        (await ReadResponseAsync<WorkflowDto>(submitResponse)).Status.Should().Be("Submitted");

        await AuthenticateAsync(client, checker.Email, checker.Password);
        var checkResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/check",
            new WorkflowActionRequest("Checked supporting valuation inputs."));
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadResponseAsync<WorkflowDto>(checkResponse)).Status.Should().Be("Checked");

        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/approve",
            new WorkflowActionRequest("Approved for publication."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedWorkflow = await ReadResponseAsync<WorkflowDto>(approveResponse);
        approvedWorkflow.Status.Should().Be("Approved");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.WorkflowActions
            .Where(action => action.WorkflowInstanceId == workflowId)
            .Select(action => action.ActionType)
            .ToListAsync();
        actions.Should().Contain(WorkflowActionType.Submitted);
        actions.Should().Contain(WorkflowActionType.Checked);
        actions.Should().Contain(WorkflowActionType.Approved);

        var auditActions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.WorkflowId == workflowId)
            .Select(auditLog => auditLog.Action)
            .ToListAsync();
        auditActions.Should().Contain("WorkflowSubmitted");
        auditActions.Should().Contain("WorkflowChecked");
        auditActions.Should().Contain("WorkflowApproved");
    }

    [Fact]
    public async Task Workflow_CanBeRejectedByDifferentUser()
    {
        using var client = _factory.CreateClient();
        var maker = await AuthenticateAsBootstrapAdminAsync(client);
        var rejector = await CreateSystemAdminDirectlyAsync(UniqueEmail("workflow-rejector"), "Workflow Rejector", "Rejector123!");
        var workflowId = await CreateWorkflowDirectlyAsync(maker.User.Id, "fee-waiver-rejection");

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/submit",
            new WorkflowActionRequest("Manual fee waiver requires rejection test."));
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, submitBody);

        await AuthenticateAsync(client, rejector.Email, rejector.Password);
        var rejectResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/reject",
            new WorkflowActionRequest("Supporting documentation is incomplete."));

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejectedWorkflow = await ReadResponseAsync<WorkflowDto>(rejectResponse);
        rejectedWorkflow.Status.Should().Be("Rejected");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var rejectedAuditExists = await dbContext.AuditLogs.AnyAsync(auditLog =>
            auditLog.WorkflowId == workflowId
            && auditLog.EventType == AuditEventType.Rejected
            && auditLog.Action == "WorkflowRejected");
        rejectedAuditExists.Should().BeTrue();
    }

    [Fact]
    public async Task Workflow_SameUserCannotSubmitAndCheck()
    {
        using var client = _factory.CreateClient();
        var maker = await AuthenticateAsBootstrapAdminAsync(client);
        var workflowId = await CreateWorkflowDirectlyAsync(maker.User.Id, "sod-violation");

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/submit",
            new WorkflowActionRequest("Submit as maker."));
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, submitBody);

        var checkResponse = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/check",
            new WorkflowActionRequest("Attempt to check own submission."));

        checkResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        checkResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await checkResponse.Content.ReadAsStringAsync();
        body.Should().MatchRegex("Segregation|same user");
    }

    [Fact]
    public async Task AuditLogs_AreAppendOnlyAtApplicationLayer()
    {
        using var scope = _factory.Services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var entityId = $"audit-immutable-{Guid.NewGuid():N}";

        await writer.WriteAsync(new AuditLogEntry(
            "Audit",
            "Created",
            "AuditProbe",
            entityId,
            EventType: AuditEventType.Created,
            ActorId: "test-user",
            Reason: "Append-only verification.",
            AfterJson: "{}"));

        var auditLog = await dbContext.AuditLogs.SingleAsync(candidate => candidate.EntityId == entityId);
        dbContext.Entry(auditLog).Property(nameof(AuditLog.Reason)).CurrentValue = "tampered";

        var action = async () => await dbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*append-only*");
    }

    [Fact]
    public async Task ImmutableArchiveRecords_CannotBeUpdatedAfterCreation()
    {
        using var scope = _factory.Services.CreateScope();
        var archiveService = scope.ServiceProvider.GetRequiredService<IImmutableArchiveService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var entityId = $"archive-{Guid.NewGuid():N}";

        var archiveRecord = await archiveService.CreateAsync(new CreateArchiveRecordRequest(
            "Archive",
            "RegulatorPack",
            entityId,
            "{\"pack\":\"Q2 regulator-ready issuance\"}",
            null,
            null,
            "Regulator-ready pack issued."));

        var storedRecord = await dbContext.ImmutableArchiveRecords.SingleAsync(candidate => candidate.Id == archiveRecord.Id);
        dbContext.Entry(storedRecord).Property(nameof(ImmutableArchiveRecord.Reason)).CurrentValue = "tampered";

        var action = async () => await dbContext.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Immutable archive records*");
    }

    private async Task<AuthTokenResponse> AuthenticateAsBootstrapAdminAsync(HttpClient client)
    {
        return await AuthenticateAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
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

    private async Task<Guid> CreateWorkflowDirectlyAsync(Guid initiatedByUserId, string entityId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var policy = await dbContext.ApprovalPolicies.SingleAsync(candidate =>
            candidate.WorkflowType == WorkflowType.NavPreparationApprovalPublication && candidate.IsActive);
        var workflow = WorkflowInstance.Create(
            WorkflowType.NavPreparationApprovalPublication,
            policy.Id,
            "NavRun",
            entityId,
            $"NAV workflow {entityId}",
            initiatedByUserId.ToString(),
            DateTime.UtcNow,
            policy.RequiresChecker,
            policy.RequiresApprover);

        dbContext.WorkflowInstances.Add(workflow);
        await dbContext.SaveChangesAsync();
        return workflow.Id;
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
