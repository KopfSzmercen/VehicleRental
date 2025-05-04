using VehicleRental.Rentals.Domain;

namespace VehicleRental.Rentals.Infrastructure;

internal static class RentalsModuleInfrastructureExtensions
{
    public static IServiceCollection AddRentalsModuleInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IRentalsRepository, InMemoryRentalsRepository>();

        return services;
    }

    public static WebApplication UseRentalsModuleInfrastructure(this WebApplication app)
    {
        return app;
    }
}