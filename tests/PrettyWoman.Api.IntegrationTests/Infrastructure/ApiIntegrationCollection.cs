namespace PrettyWoman.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiIntegrationCollection : ICollectionFixture<PrettyWomanApiFactory>
{
    public const string Name = "API integration";
}
