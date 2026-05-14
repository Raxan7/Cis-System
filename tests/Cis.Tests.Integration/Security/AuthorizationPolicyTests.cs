using Cis.Application.Common.Security;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cis.Tests.Integration.Security;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthorizationPolicyTests
{
    private readonly CisApiFactory _factory;

    public AuthorizationPolicyTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void RequiredPolicies_AreRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        var authorizationOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        authorizationOptions.GetPolicy(AuthorizationPolicyNames.Maker).Should().NotBeNull();
        authorizationOptions.GetPolicy(AuthorizationPolicyNames.Checker).Should().NotBeNull();
        authorizationOptions.GetPolicy(AuthorizationPolicyNames.Approver).Should().NotBeNull();
        authorizationOptions.GetPolicy(AuthorizationPolicyNames.Auditor).Should().NotBeNull();
        authorizationOptions.GetPolicy(AuthorizationPolicyNames.SystemAdministrator).Should().NotBeNull();
    }
}
