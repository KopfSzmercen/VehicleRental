using Testcontainers.PostgreSql;

namespace VehicleRental.Tests.Integration;

internal sealed class TestPostgresDb : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithPassword("Password123")
        .WithUsername("postgres")
        .Build();

    public string ConnectionString { get; private set; } = null!;


    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}