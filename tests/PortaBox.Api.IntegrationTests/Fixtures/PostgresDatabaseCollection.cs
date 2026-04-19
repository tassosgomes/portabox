using PortaBox.Api.IntegrationTests.Fixtures;

namespace PortaBox.Api.IntegrationTests;

[CollectionDefinition(nameof(PostgresDatabaseCollection))]
public sealed class PostgresDatabaseCollection : ICollectionFixture<PostgresDatabaseFixture>;
