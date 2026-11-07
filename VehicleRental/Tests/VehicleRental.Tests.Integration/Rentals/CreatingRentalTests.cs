using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Reservations;
using VehicleRental.Rentals.Endpoints;
using VehicleRental.Rentals.Endpoints.Reservations;

namespace VehicleRental.Tests.Integration.Rentals;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class CreatingRentalTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenValidRequestAndUserIsSignedIn_CreateRental_ShouldSucceed()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();

        // Act
        var response = await client.PostAsJsonAsync("/rentals", ValidRequestFor(vehicleId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenVehicleInVerification_CreateRental_ShouldReturnBadRequestAndNotSaveRental()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var vehicleId = await testWebApplication.SeedVehicleInVerificationAsync();

        // Act
        var response = await client.PostAsJsonAsync("/rentals", ValidRequestFor(vehicleId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe("Vehicle is not available for rental.");

        using (var assertScope = testWebApplication.Services.CreateScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var rentalVehicle = await dbContext.RentalVehicles
                .Where(rv => rv.Id == vehicleId)
                .Include(x => x.Rental)
                .SingleAsync();

            rentalVehicle.Rental.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GivenOwnerRentsReservedVehicleInsideWindow_CreateRental_ShouldFulfilReservation()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationStart = DateTimeOffset.UtcNow.AddMinutes(3);
        var reservationId = await ReserveAsync(owner, vehicleId, reservationStart);

        // Act
        var response = await owner.PostAsJsonAsync("/rentals", new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = reservationStart.AddMinutes(10),
            EndDate = reservationStart.AddDays(1)
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rentalId = await response.Content.ReadFromJsonAsync<Guid>();

        var reservation = await testWebApplication.GetReservationAsync(reservationId);
        reservation.Status.ShouldBe(ReservationStatus.Fulfilled);
        reservation.CurrentVehicleId.ShouldBeNull();

        var rentalVehicle = await LoadRentalsVehicleAsync(vehicleId);
        rentalVehicle.Rental.ShouldNotBeNull();
        rentalVehicle.Rental.Id.ShouldBe(rentalId);
        rentalVehicle.Reservation.ShouldBeNull();
    }

    [Fact]
    public async Task GivenOwnerRentsReservedVehicleOutsideWindow_CreateRental_ShouldReturnBadRequestAndChangeNothing()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationStart = DateTimeOffset.UtcNow.AddMinutes(3);
        var reservationId = await ReserveAsync(owner, vehicleId, reservationStart);

        // Act
        var response = await owner.PostAsJsonAsync("/rentals", new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = reservationStart.AddHours(2),
            EndDate = reservationStart.AddDays(1)
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<string>())
            .ShouldBe("Rental must start within the reservation window.");

        (await testWebApplication.GetReservationAsync(reservationId)).Status.ShouldBe(ReservationStatus.Active);

        var rentalVehicle = await LoadRentalsVehicleAsync(vehicleId);
        rentalVehicle.Rental.ShouldBeNull();
        rentalVehicle.Reservation.ShouldNotBeNull();
        rentalVehicle.Reservation.Id.ShouldBe(reservationId);
    }

    [Fact]
    public async Task GivenVehicleReservedByAnotherClient_CreateRental_ShouldReturnBadRequest()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var otherClient = await testWebApplication.CreateClient().SignInAsAdminAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationStart = DateTimeOffset.UtcNow.AddMinutes(3);
        await ReserveAsync(owner, vehicleId, reservationStart);

        // Act
        var response = await otherClient.PostAsJsonAsync("/rentals", new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = reservationStart.AddMinutes(10),
            EndDate = reservationStart.AddDays(1)
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe("Vehicle is reserved.");
        (await LoadRentalsVehicleAsync(vehicleId)).Rental.ShouldBeNull();
    }

    private static async Task<Guid> ReserveAsync(HttpClient client, Guid vehicleId, DateTimeOffset startDate)
    {
        var response = await client.PostAsJsonAsync("/reservations", new CreateReservationEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = startDate,
            DurationInSeconds = 3600
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<RentalsVehicle> LoadRentalsVehicleAsync(Guid vehicleId)
    {
        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.RentalVehicles
            .AsNoTracking()
            .Include(x => x.Rental)
            .Include(x => x.Reservation)
            .SingleAsync(x => x.Id == vehicleId);
    }

    private static CreateRentalEndpoint.Request ValidRequestFor(Guid vehicleId)
    {
        return new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = DateTimeOffset.Now.AddMinutes(3),
            EndDate = DateTimeOffset.Now.AddDays(1)
        };
    }
}
