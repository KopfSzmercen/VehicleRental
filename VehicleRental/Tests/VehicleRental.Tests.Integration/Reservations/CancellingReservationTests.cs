using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Rentals.Domain.Reservations;
using VehicleRental.Rentals.Endpoints.Reservations;

namespace VehicleRental.Tests.Integration.Reservations;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class CancellingReservationTests(TestWebApplication testWebApplication) : IDisposable
{
    private const string OwnerEmail = "user@t.pl";

    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenOwnerCancelsActiveReservation_CancelReservation_ShouldCancelAndFreeVehicle()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var otherClient = await testWebApplication.CreateClient().SignInAsAdminAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationId = await CreateReservationAsync(owner, vehicleId);

        // Act
        var response = await owner.PutAsync($"/reservations/{reservationId}/cancel", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Guid>()).ShouldBe(reservationId);

        var reservation = await testWebApplication.GetReservationAsync(reservationId);
        reservation.Status.ShouldBe(ReservationStatus.Cancelled);
        reservation.CancelledAt.ShouldNotBeNull();
        reservation.CurrentVehicleId.ShouldBeNull();

        var reserveAgain = await otherClient.PostAsJsonAsync("/reservations", ValidRequestFor(vehicleId));
        reserveAgain.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenAnotherClientsReservation_CancelReservation_ShouldReturnNotFound()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var otherClient = await testWebApplication.CreateClient().SignInAsAdminAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationId = await CreateReservationAsync(owner, vehicleId);

        // Act
        var response = await otherClient.PutAsync($"/reservations/{reservationId}/cancel", null);

        // Assert
        await ShouldBeNotFoundAsync(response, reservationId);
        (await testWebApplication.GetReservationAsync(reservationId)).Status.ShouldBe(ReservationStatus.Active);
    }

    [Fact]
    public async Task GivenUnknownReservationId_CancelReservation_ShouldReturnNotFound()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var unknownId = Guid.NewGuid();

        // Act
        var response = await owner.PutAsync($"/reservations/{unknownId}/cancel", null);

        // Assert
        await ShouldBeNotFoundAsync(response, unknownId);
    }

    [Fact]
    public async Task GivenAlreadyCancelledReservation_CancelReservation_ShouldReturnNotFound()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationId = await CreateReservationAsync(owner, vehicleId);
        (await owner.PutAsync($"/reservations/{reservationId}/cancel", null)).EnsureSuccessStatusCode();

        // Act
        var response = await owner.PutAsync($"/reservations/{reservationId}/cancel", null);

        // Assert
        await ShouldBeNotFoundAsync(response, reservationId);
        (await testWebApplication.GetReservationAsync(reservationId)).Status.ShouldBe(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task GivenExpiredReservation_CancelReservation_ShouldReturnNotFoundAndStoreItAsExpired()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var ownerId = await testWebApplication.GetUserIdAsync(OwnerEmail);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var reservationId = await testWebApplication.SeedExpiredReservationAsync(vehicleId, ownerId);

        // Act
        var response = await owner.PutAsync($"/reservations/{reservationId}/cancel", null);

        // Assert
        await ShouldBeNotFoundAsync(response, reservationId);

        var reservation = await testWebApplication.GetReservationAsync(reservationId);
        reservation.Status.ShouldBe(ReservationStatus.Expired);
        reservation.CurrentVehicleId.ShouldBeNull();
    }

    [Fact]
    public async Task GivenUserIsNotSignedIn_CancelReservation_ShouldReturnUnauthorized()
    {
        var response = await testWebApplication.CreateClient()
            .PutAsync($"/reservations/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<Guid> CreateReservationAsync(HttpClient client, Guid vehicleId)
    {
        var response = await client.PostAsJsonAsync("/reservations", ValidRequestFor(vehicleId));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task ShouldBeNotFoundAsync(HttpResponseMessage response, Guid reservationId)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<string>())
            .ShouldBe($"Reservation with ID {reservationId} does not exist.");
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
