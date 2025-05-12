using Microsoft.EntityFrameworkCore;

namespace VehicleRental.Persistence;

internal static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"));
        });

        services.AddDbContext<AppReadDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    public static async Task ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}