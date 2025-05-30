using FluentValidation;
using VehicleRental.Rentals.Endpoints;
using VehicleRental.Rentals.Endpoints.Reservations;
using VehicleRental.Rentals.Infrastructure;

namespace VehicleRental.Rentals;

internal static class RentalsModule
{
    public static IServiceCollection AddRentalsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(RentalsModule).Assembly, includeInternalTypes: true);
        services.AddRentalsModuleInfrastructure(configuration);

        return services;
    }

    public static WebApplication UseRentalsModule(this WebApplication app)
    {
        app.AddRentalsEndpoints();
        app.AddReservationsEndpoints();
        return app;
    }
}