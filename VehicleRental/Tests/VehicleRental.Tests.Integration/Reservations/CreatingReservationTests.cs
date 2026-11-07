using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain.Reservations;
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

        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();

        var response = await client.PostAsJsonAsync("/reservations", ValidRequestFor(vehicleId));
        response.EnsureSuccessStatusCode();

        using (var assertScope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = assertScope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            var rentalVehicle = await dbContext.RentalVehicles
                .Where(rv => rv.Id == vehicleId)
                .Include(x => x.Reservation)
                .SingleAsync();

            rentalVehicle.Reservation.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GivenVehicleInVerification_CreateReservation_ShouldReturnBadRequestAndNotSaveReservation()
    {
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var vehicleId = await testWebApplication.SeedVehicleInVerificationAsync();

        var response = await client.PostAsJsonAsync("/reservations", ValidRequestFor(vehicleId));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe("Vehicle is not available for rental.");

        using (var assertScope = testWebApplication.Services.CreateScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var rentalVehicle = await dbContext.RentalVehicles
                .Where(rv => rv.Id == vehicleId)
                .Include(x => x.Reservation)
                .SingleAsync();

            rentalVehicle.Reservation.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GivenVehicleWithExpiredReservation_CreateReservation_ShouldSucceedAndStoreOldOneAsExpired()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var previousOwnerId = await testWebApplication.GetUserIdAsync("admin@admin.com");
        var expiredReservationId = await testWebApplication.SeedExpiredReservationAsync(vehicleId, previousOwnerId);

        // Act
        var response = await client.PostAsJsonAsync("/reservations", ValidRequestFor(vehicleId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newReservationId = await response.Content.ReadFromJsonAsync<Guid>();

        var expiredReservation = await testWebApplication.GetReservationAsync(expiredReservationId);
        expiredReservation.Status.ShouldBe(ReservationStatus.Expired);
        expiredReservation.CurrentVehicleId.ShouldBeNull();

        var newReservation = await testWebApplication.GetReservationAsync(newReservationId);
        newReservation.Status.ShouldBe(ReservationStatus.Active);
        newReservation.CurrentVehicleId.ShouldBe(vehicleId);
    }

    [Fact]
    public async Task GivenCustomerHasUnpaidRental_CreateReservation_ShouldReturnBadRequestAndNotSaveReservation()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        await testWebApplication.CreateUnpaidRentalAsync(client, await testWebApplication.SeedAvailableVehicleAsync());
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();

        // Act
        var response = await client.PostAsJsonAsync("/reservations", ValidRequestFor(vehicleId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe("Customer has unpaid rentals.");

        using var assertScope = testWebApplication.Services.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rentalVehicle = await dbContext.RentalVehicles
            .Where(rv => rv.Id == vehicleId)
            .Include(x => x.Reservation)
            .SingleAsync();

        rentalVehicle.Reservation.ShouldBeNull();
    }

    private static CreateReservationEndpoint.Request ValidRequestFor(Guid vehicleId)
    {
        return new CreateReservationEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = DateTimeOffset.Now.AddMinutes(3),
            DurationInSeconds = 3600
        };
    }
}
