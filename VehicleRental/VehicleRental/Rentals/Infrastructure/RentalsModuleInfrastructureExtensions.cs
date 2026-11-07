using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;
using VehicleRental.Rentals.Infrastructure.Payments;

namespace VehicleRental.Rentals.Infrastructure;

internal static class RentalsModuleInfrastructureExtensions
{
    public static IServiceCollection AddRentalsModuleInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<IRentalsVehicleRepository, RentalsVehicleRepository>();
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        return services;
    }

    public static WebApplication UseRentalsModuleInfrastructure(this WebApplication app)
    {
        return app;
    }
}