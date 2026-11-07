using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;
using VehicleRental.Rentals.Domain.Reservations;
using VehicleRental.Rentals.Endpoints;
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
            userId,
            customerHasUnpaidRentals: false
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

    // Creates a 2-hour rental (price 20 USD) through HTTP and finishes it while the gateway declines the charge,
    // so the client's customer ends up with an outstanding payment. The gateway is set back to Succeeded.
    public static async Task<Guid> CreateUnpaidRentalAsync(
        this TestWebApplication testWebApplication,
        HttpClient client,
        Guid vehicleId)
    {
        var startDate = DateTimeOffset.UtcNow.AddMinutes(3);

        var createResponse = await client.PostAsJsonAsync("/rentals", new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = startDate,
            EndDate = startDate.AddHours(2)
        });
        createResponse.EnsureSuccessStatusCode();
        var rentalId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        testWebApplication.PaymentGateway.ChargeResult = ChargeResult.Declined;
        var finishResponse = await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());
        finishResponse.EnsureSuccessStatusCode();
        testWebApplication.PaymentGateway.ChargeResult = ChargeResult.Succeeded;

        return rentalId;
    }

    public static async Task<Rental> GetRentalAsync(this TestWebApplication testWebApplication, Guid rentalId)
    {
        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Set<Rental>()
            .AsNoTracking()
            .SingleAsync(r => r.Id == rentalId);
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
