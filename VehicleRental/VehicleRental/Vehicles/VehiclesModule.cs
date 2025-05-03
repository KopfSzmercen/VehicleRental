using FluentValidation;
using VehicleRental.Persistence;
using VehicleRental.Vehicles.Domain.Vehicles;
using VehicleRental.Vehicles.Endpoints;
using VehicleRental.Vehicles.Infrastructure;

namespace VehicleRental.Vehicles;

public static class VehiclesModule
{
    public static IServiceCollection AddVehiclesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IVehicleRepository, InMemoryVehiclesRepository>();

        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

        services.AddValidatorsFromAssemblyContaining<CreateVehicleEndpoint.RequestValidator>(ServiceLifetime.Singleton);

        return services;
    }

    public static WebApplication UseVehiclesModule(this WebApplication app)
    {
        app.AddVehiclesEndpoints();
        return app;
    }
}