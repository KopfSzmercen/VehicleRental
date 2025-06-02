using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Endpoints.Reservations;

namespace VehicleRental.Tests.Integration.Reservations;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class CreatingReservationTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenValidRequestAndUserIsSignedIn_CreateReservation_ShouldSucceed()
    {
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var vehicle = RentalsVehicle.CreateNew(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow
        );

        using (var scope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            dbContext.RentalVehicles.Add(vehicle);
            await dbContext.SaveChangesAsync();
        }

        var createReservationRequest = new CreateReservationEndpoint.Request
        {
            VehicleId = vehicle.Id,
            StartDate = DateTimeOffset.Now.AddMinutes(3),
            DurationInSeconds = 3600
        };

        var response = await client.PostAsJsonAsync("/reservations", createReservationRequest);
        response.EnsureSuccessStatusCode();

        using (var assertScope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = assertScope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            var rentalVehicle = await dbContext.RentalVehicles
                .Where(rv => rv.Id == vehicle.Id)
                .Include(x => x.Reservation)
                .SingleAsync();

            rentalVehicle.Reservation.ShouldNotBeNull();
        }
    }
}