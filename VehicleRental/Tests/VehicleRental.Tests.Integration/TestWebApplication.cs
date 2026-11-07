using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using VehicleRental.Rentals.Domain.Payments;

namespace VehicleRental.Tests.Integration;

public class TestWebApplication : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestPostgresDb _database = new();

    public HttpClient HttpClient { get; private set; } = null!;

    internal StubPaymentGateway PaymentGateway { get; } = new();

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

        PaymentGateway.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:Database",
            _database.ConnectionString
        );

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(PaymentGateway);
        });
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