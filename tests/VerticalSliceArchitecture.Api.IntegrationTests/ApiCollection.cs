namespace VerticalSliceArchitecture.Api.IntegrationTests;

/// <summary>Shares one <see cref="ApiFactory"/> (and its Postgres container) across a test class.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
