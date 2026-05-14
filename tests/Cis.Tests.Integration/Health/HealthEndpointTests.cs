using System.Net;
using FluentAssertions;
using Cis.Tests.Integration.Support;

namespace Cis.Tests.Integration.Health;

[Collection(IntegrationTestCollection.Name)]
public sealed class HealthEndpointTests
{
    private readonly CisApiFactory _factory;

    public HealthEndpointTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_WhenPostgresIsAvailable_ReturnsHealthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
        body.Should().Contain("postgresql");
    }
}
