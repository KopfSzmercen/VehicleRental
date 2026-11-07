using Microsoft.Extensions.DependencyInjection;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Vehicles.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleRental.Tests.Integration;

internal static class TestWebApplicationExtensions
{
    // Seeds a Vehicle (Vehicles module) and a RentalsVehicle (Rentals module) with the same id.
    public static Task<Guid> SeedAvailableVehicleAsync(this TestWebApplication testWebApplication)
    {
        return SeedVehicleAsync(testWebApplication, makeAvailable: true);
    }

    public static Task<Guid> SeedVehicleInVerificationAsync(this TestWebApplication testWebApplication)
    {
        return SeedVehicleAsync(testWebApplication, makeAvailable: false);
    }

    private static async Task<Guid> SeedVehicleAsync(TestWebApplication testWebApplication, bool makeAvailable)
    {
        var now = DateTimeOffset.UtcNow;

        var vehicle = Vehicle.CreateNew(
            "Test Vehicle",
            "TEST1234",
            GeoLocalization.Create(50.0, 19.0),
            true,
            now
        );

        if (makeAvailable)
        {
            vehicle.AddLegalDocument(VehicleLegalDocument.CreateNew("Insurance", vehicle.Id, now.AddYears(1), now));
            vehicle.MakeAvailable(now);
        }

        var rentalsVehicle = RentalsVehicle.CreateNew(vehicle.Id, now);

        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Vehicles.Add(vehicle);
        dbContext.RentalVehicles.Add(rentalsVehicle);
        await dbContext.SaveChangesAsync();

        return vehicle.Id;
    }
}
