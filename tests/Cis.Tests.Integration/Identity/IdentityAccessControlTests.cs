using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public sealed class IdentityAccessControlTests
{
    private readonly CisApiFactory _factory;

    public IdentityAccessControlTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsProblemDetailsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Authentication is required");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAuthenticatedUserMissingPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var createdUser = await CreateUserAsync(client, UniqueEmail("no-permission"), "No Permission User", "NoPermission123!");

        var lowPrivilegeToken = await LoginAsync(client, createdUser.Email, "NoPermission123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lowPrivilegeToken.AccessToken);

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task RoleAssignment_RemainsPendingUntilApprovedByDifferentUser()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var targetUser = await CreateUserAsync(client, UniqueEmail("role-target"), "Role Target User", "RoleTarget123!");

        var requestResponse = await client.PostAsJsonAsync(
            $"/api/users/{targetUser.Id}/roles",
            new AssignRolesRequest([RoleNames.InternalAuditor], "Grant internal auditor access for test control."));
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var accessChangeRequest = await ReadResponseAsync<AccessChangeRequestDto>(requestResponse);
        accessChangeRequest.Status.Should().Be("Pending");

        await UserShouldNotHaveRoleAsync(targetUser.Id, RoleNames.InternalAuditor);

        var selfApprovalResponse = await client.PostAsJsonAsync(
            $"/api/access-change-requests/{accessChangeRequest.Id}/approve",
            new AccessChangeDecisionRequest("Attempted same-user approval should be blocked."));
        selfApprovalResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var approverEmail = await CreateSystemAdminDirectlyAsync(UniqueEmail("approver"), "Approver User", "Approver123!");
        var approverToken = await LoginAsync(client, approverEmail, "Approver123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approverToken.AccessToken);

        var approvalResponse = await client.PostAsJsonAsync(
            $"/api/access-change-requests/{accessChangeRequest.Id}/approve",
            new AccessChangeDecisionRequest("Approved by a separate control owner."));
        approvalResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedRequest = await ReadResponseAsync<AccessChangeRequestDto>(approvalResponse);
        approvedRequest.Status.Should().Be("Approved");
        await UserShouldHaveRoleAsync(targetUser.Id, RoleNames.InternalAuditor);
    }

    [Fact]
    public async Task UserCreationAndAccessWorkflow_WriteAuditLogs()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var targetUser = await CreateUserAsync(client, UniqueEmail("audit-target"), "Audit Target User", "AuditTarget123!");

        var requestResponse = await client.PostAsJsonAsync(
            $"/api/users/{targetUser.Id}/roles",
            new AssignRolesRequest([RoleNames.ExternalAuditor], "Grant external auditor access for audit test."));
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.Module == "Identity")
            .Select(auditLog => auditLog.Action)
            .ToListAsync();

        actions.Should().Contain("Login");
        actions.Should().Contain("UserCreated");
        actions.Should().Contain("AccessChangeRequested");
        actions.Should().Contain("PrivilegeEscalationRequested");
    }

    [Fact]
    public async Task FailedLogin_WritesAuditLog()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail("missing-login");

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var auditExists = await dbContext.AuditLogs.AnyAsync(auditLog =>
            auditLog.Module == "Identity"
            && auditLog.Action == "FailedLogin"
            && auditLog.ActorId == email);
        auditExists.Should().BeTrue();
    }

    private async Task AuthenticateAsBootstrapAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
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

    private async Task<string> CreateSystemAdminDirectlyAsync(string email, string displayName, string password)
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
        return email;
    }

    private async Task UserShouldHaveRoleAsync(Guid userId, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var hasRole = await dbContext.UserRoles.AnyAsync(userRole => userRole.UserId == userId && userRole.Role!.Name == roleName);
        hasRole.Should().BeTrue();
    }

    private async Task UserShouldNotHaveRoleAsync(Guid userId, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var hasRole = await dbContext.UserRoles.AnyAsync(userRole => userRole.UserId == userId && userRole.Role!.Name == roleName);
        hasRole.Should().BeFalse();
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
