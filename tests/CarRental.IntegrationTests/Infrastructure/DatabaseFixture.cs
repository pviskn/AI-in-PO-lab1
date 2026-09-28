using Testcontainers.PostgreSql;
using Xunit;

namespace CarRental.IntegrationTests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithDatabase("carrental_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}
