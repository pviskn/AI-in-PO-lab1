using Xunit;

namespace CarRental.IntegrationTests.Infrastructure;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<DatabaseFixture>
{
}