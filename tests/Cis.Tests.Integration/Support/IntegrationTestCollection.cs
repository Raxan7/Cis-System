namespace Cis.Tests.Integration.Support;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CisApiFactory>
{
    public const string Name = "Integration";
}
