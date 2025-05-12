using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;

namespace VehicleRental.Tests.Integration;

public class TestWebApplication : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestPostgresDb _database = new();

    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        HttpClient = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        var postgresConnection = new NpgsqlConnection(_database.ConnectionString);

        await postgresConnection.OpenAsync();

        var respawner = await Respawner.CreateAsync(postgresConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore =
            [
                "__EFMigrationsHistory",
                "AspNetRoleClaims",
                "AspNetRoles"
            ]
        });

        await respawner.ResetAsync(postgresConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:Database",
            _database.ConnectionString
        );
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Tokens:Secret", "supersecret123456789abcdefghijkl" },
                { "Tokens:Audience", "VehicleRentalIntegrationTests" },
                { "Tokens:Issuer", "VehicleRentalIntegrationTests" }
            }!);
        });

        return base.CreateHost(builder);
    }
}