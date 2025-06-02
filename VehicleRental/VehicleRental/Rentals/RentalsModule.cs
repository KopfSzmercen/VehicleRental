using FluentValidation;
using VehicleRental.Common.Messaging;
using VehicleRental.IntegrationEvents;
using VehicleRental.Rentals.Endpoints;
using VehicleRental.Rentals.Endpoints.Reservations;
using VehicleRental.Rentals.Infrastructure;
using VehicleRental.Rentals.IntegrationEventHandlers;

namespace VehicleRental.Rentals;

internal static class RentalsModule
{
    public static IServiceCollection AddRentalsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(RentalsModule).Assembly, includeInternalTypes: true);
        services.AddRentalsModuleInfrastructure(configuration);

        services
            .AddScoped<IIntegrationEventHandler<VehicleCreatedIntegrationEvent>,
                VehicleCreatedIntegrationEventHandler>();

        return services;
    }

    public static WebApplication UseRentalsModule(this WebApplication app)
    {
        app.AddRentalsEndpoints();
        app.AddReservationsEndpoints();
        return app;
    }
}