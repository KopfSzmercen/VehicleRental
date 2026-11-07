using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;
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

    // Attaches a reservation whose window ended in the past, without marking it expired.
    // Expiry can't be reached in real time through HTTP, so it is seeded through the domain with a past `now`.
    public static async Task<Guid> SeedExpiredReservationAsync(
        this TestWebApplication testWebApplication,
        Guid vehicleId,
        Guid userId)
    {
        var pastNow = DateTimeOffset.UtcNow.AddHours(-3);

        var reservation = Reservation.CreateNew(
            vehicleId,
            pastNow.AddMinutes(1),
            new ReservationDurationInSeconds(3600),
            pastNow,
            userId
        );

        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rentalsVehicle = await dbContext.RentalVehicles
            .Include(x => x.Reservation)
            .Include(x => x.Rental)
            .SingleAsync(x => x.Id == vehicleId);

        rentalsVehicle.Reserve(reservation, true, pastNow);
        await dbContext.SaveChangesAsync();

        return reservation.Id;
    }

    public static async Task<Guid> GetUserIdAsync(this TestWebApplication testWebApplication, string email)
    {
        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .SingleAsync();
    }

    public static async Task<Reservation> GetReservationAsync(
        this TestWebApplication testWebApplication,
        Guid reservationId)
    {
        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Set<Reservation>()
            .AsNoTracking()
            .SingleAsync(r => r.Id == reservationId);
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
