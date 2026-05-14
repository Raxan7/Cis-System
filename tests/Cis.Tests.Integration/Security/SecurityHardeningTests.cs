using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Investors;
using Cis.Contracts.Portal;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Portal;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Infrastructure.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Cis.Tests.Integration.Security;

[Collection(IntegrationTestCollection.Name)]
public sealed class SecurityHardeningTests
{
    private readonly CisApiFactory _factory;

    public SecurityHardeningTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnauthorizedAndForbiddenRequests_AreRejected_AndAudited()
    {
        using var client = _factory.CreateClient();

        var unauthorized = await client.GetAsync("/api/users");
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var lowPrivilegeUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("board"), "Board User", "BoardUser123!", RoleNames.BoardUser);
        await AuthenticateAsync(client, lowPrivilegeUser.Email, lowPrivilegeUser.Password);

        var forbidden = await client.GetAsync("/api/users");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Security" && log.Action == "AuthenticationChallenged")).Should().BeTrue();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Security" && log.Action == "AuthorizationForbidden")).Should().BeTrue();
    }

    [Fact]
    public async Task PortalAccess_IsScopedToOwnInvestorRecords()
    {
        using var client = _factory.CreateClient();
        var own = await SeedPortalInvestorAsync("OWNSEC", mfaRequired: true, mfaVerified: true);
        var other = await SeedPortalInvestorAsync("OTHERSEC", mfaRequired: false, mfaVerified: false);
        await AuthenticateAsync(client, own.Email, own.Password);

        var response = await client.GetAsync("/api/portal/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transactions = await ReadResponseAsync<IReadOnlyCollection<PortalTransactionDto>>(response);
        transactions.Should().Contain(transaction => transaction.Reference == own.TransactionReference);
        transactions.Should().NotContain(transaction => transaction.Reference == other.TransactionReference);
    }

    [Fact]
    public async Task SameUser_CannotApproveOwnAccessEscalation()
    {
        using var client = _factory.CreateClient();
        await AuthenticateBootstrapAdminAsync(client);
        var targetUser = await CreateUserAsync(client, UniqueEmail("acr-target"), "ACR Target", "AcrTarget123!");

        var requestResponse = await client.PostAsJsonAsync(
            $"/api/users/{targetUser.Id}/roles",
            new AssignRolesRequest([RoleNames.InternalAuditor], "Segregation of duties test."));
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accessChangeRequest = await ReadResponseAsync<AccessChangeRequestDto>(requestResponse);

        var selfApprovalResponse = await client.PostAsJsonAsync(
            $"/api/access-change-requests/{accessChangeRequest.Id}/approve",
            new AccessChangeDecisionRequest("Self approval should fail."));

        selfApprovalResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MaliciousFileUpload_IsRejected()
    {
        using var client = _factory.CreateClient();
        await AuthenticateBootstrapAdminAsync(client);
        var investor = await CreateInvestorAsync(client, "malicious-upload");

        var response = await client.PostAsJsonAsync(
            $"/api/investors/{investor.Id}/documents",
            new AddKycDocumentRequest(
                "NationalId",
                "payload.exe",
                "application/x-msdownload",
                2048,
                "kyc/malware/payload.exe",
                null,
                null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.KycDocuments.CountAsync(document => document.InvestorId == investor.Id)).Should().Be(0);
    }

    [Fact]
    public async Task RepeatedLoginFailures_LockAccount()
    {
        using var client = _factory.CreateClient();
        var user = await CreateUserWithRoleDirectlyAsync(UniqueEmail("lockout"), "Lockout User", "Lockout123!", RoleNames.InvestorPortalUser);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "WrongPassword123!"));
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var correctPasswordResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, user.Password));
        correctPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Email == user.Email);
        persistedUser.Status.Should().Be(UserStatus.Locked);
        persistedUser.LockoutEndUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(expiresAtUtc: DateTime.UtcNow.AddMinutes(-10), signingKeyMutator: null));

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedJwt_IsRejected()
    {
        using var client = _factory.CreateClient();
        await AuthenticateBootstrapAdminAsync(client);
        var validToken = (await LoginAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!")).AccessToken;
        var tamperedToken = validToken[..^2] + (validToken[^1] == 'a' ? "b" : "a");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PrivilegedMfa_CanBeEnforcedByConfiguration()
    {
        using var strictFactory = CreateStrictMfaFactory();
        using var client = strictFactory.CreateClient();

        var withoutMfa = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin.tests@victoryfs.local", "ChangeMe123!"));
        withoutMfa.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using (var scope = strictFactory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
            var bootstrapAdmin = await dbContext.Users.SingleAsync(user => user.Email == "admin.tests@victoryfs.local");
            bootstrapAdmin.SetMfa(true, DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        var withMfa = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin.tests@victoryfs.local", "ChangeMe123!", "000000"));
        withMfa.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void OnlyExpectedControllerEndpoints_AllowAnonymous()
    {
        var allowAnonymousActions = typeof(Program).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .OrderBy(name => name)
            .ToArray();

        allowAnonymousActions.Should().Equal("AuthController.Login", "AuthController.Refresh");
    }

    private WebApplicationFactory<Program> CreateStrictMfaFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:Mfa:RequireForPrivilegedRoles"] = "true"
                });
            });
        });
    }

    private async Task AuthenticateBootstrapAdminAsync(HttpClient client)
    {
        var token = await LoginAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    private static async Task<AuthTokenResponse> LoginAsync(HttpClient client, string email, string password, string? mfaCode = null)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, mfaCode));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return await ReadResponseAsync<AuthTokenResponse>(response);
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, string password, string? mfaCode = null)
    {
        var token = await LoginAsync(client, email, password, mfaCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    private static async Task<UserDto> CreateUserAsync(HttpClient client, string email, string displayName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(email, displayName, password));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<UserDto>(response);
    }

    private async Task<InvestorDto> CreateInvestorAsync(HttpClient client, string prefix)
    {
        var request = new CreateInvestorRequest(
            "Individual",
            $"Investor {prefix}",
            UniqueEmail(prefix),
            "+254700000001",
            "Low",
            "Low risk for security upload test.",
            $"ID-{Guid.NewGuid():N}"[..18],
            "Test",
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
            $"TAX-{Guid.NewGuid():N}"[..16],
            "Kenya",
            "Nairobi",
            "Standard",
            "Self",
            new DateOnly(2026, 1, 1),
            null);

        var response = await client.PostAsJsonAsync("/api/investors", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return await ReadResponseAsync<InvestorDto>(response);
    }

    private async Task<(Guid Id, string Email, string Password)> CreateUserWithRoleDirectlyAsync(string email, string displayName, string password, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var role = await dbContext.Roles.SingleAsync(candidate => candidate.Name == roleName);
        var now = DateTime.UtcNow;
        var user = User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private async Task<PortalSeed> SeedPortalInvestorAsync(string prefix, bool mfaRequired, bool mfaVerified)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var user = await CreateUserWithRoleDirectlyAsync(UniqueEmail($"portal-{prefix.ToLowerInvariant()}"), $"{prefix} Portal User", "Portal123!", RoleNames.InvestorPortalUser);
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-{prefix}-{Guid.NewGuid():N}"[..30], InvestorType.Individual, $"{prefix} Investor", user.Email, "+254700000000", "test-fixture", now);
        var scheme = Scheme.Create($"SCH-{prefix}-{Guid.NewGuid():N}"[..30], $"{prefix} Scheme", "Unit Trust", "KES", "test-fixture", now);
        var schemeClass = scheme.AddClass("CLS", "Default Class", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(15, 0), 100m, 10m, 0, 0);

        dbContext.Investors.Add(investor);
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();

        dbContext.PortalUserProfiles.Add(PortalUserProfile.Create(user.Id, investor.Id, investor.DisplayName, investor.Email, "test-fixture", now));
        dbContext.PortalMfaSettings.Add(PortalMfaSetting.Create(user.Id, mfaRequired, mfaVerified, "test-fixture", now));

        var transactionReference = $"PORTAL-{prefix}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        var source = UnitMovementSource.Create(UnitMovementSourceType.Adjustment, Guid.NewGuid(), investor.Id, scheme.Id, schemeClass.Id, transactionReference, true, "test-fixture", now);
        var entry = UnitLedgerEntry.Create(UnitMovementType.Issued, investor.Id, scheme.Id, schemeClass.Id, Domain.Common.BusinessDate.From(new DateOnly(2026, 10, 1)), 125.25m, source, transactionReference, 4, "test-fixture", now, "Portal seed holding.");
        var holding = UnitHolding.Create(investor.Id, scheme.Id, schemeClass.Id, 4, "test-fixture", now);
        holding.Apply(entry);
        dbContext.UnitMovementSources.Add(source);
        dbContext.UnitLedgerEntries.Add(entry);
        dbContext.UnitHoldings.Add(holding);
        await dbContext.SaveChangesAsync();

        return new PortalSeed(user.Id, user.Email, user.Password, investor.Id, scheme.Id, schemeClass.Id, transactionReference);
    }

    private string CreateJwt(DateTime expiresAtUtc, Func<string, string>? signingKeyMutator)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtOptions = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var signingKeyValue = signingKeyMutator?.Invoke(jwtOptions.SigningKey) ?? jwtOptions.SigningKey;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyValue));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(PermissionClaimTypes.Permission, Permissions.Identity.UsersRead)
        };
        var token = new JwtSecurityToken(jwtOptions.Issuer, jwtOptions.Audience, claims, now.AddMinutes(-30), expiresAtUtc, credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
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

    private sealed record PortalSeed(Guid UserId, string Email, string Password, Guid InvestorId, Guid SchemeId, Guid SchemeClassId, string TransactionReference);
}
