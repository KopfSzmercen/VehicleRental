using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;
using VehicleRental.Rentals.Endpoints;
using VehicleRental.Rentals.Endpoints.Reservations;

namespace VehicleRental.Tests.Integration.Rentals;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class SettlingRentalPaymentTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenUnpaidRentalAndChargeSucceeds_SettlePayment_ShouldMarkPaidAndLiftTheBlock()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var customerId = await testWebApplication.GetUserIdAsync("user@t.pl");
        var rentalId = await testWebApplication.CreateUnpaidRentalAsync(
            client, await testWebApplication.SeedAvailableVehicleAsync());

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<Guid>()).ShouldBe(rentalId);

        var rental = await testWebApplication.GetRentalAsync(rentalId);
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        rental.PaidAt.ShouldNotBeNull();

        var charge = testWebApplication.PaymentGateway.Charges.ShouldHaveSingleItem();
        charge.CustomerId.ShouldBe(customerId);
        charge.Amount.ShouldBe(new Money(20, Currency.USD));
        charge.IdempotencyKey.ShouldBe(rentalId);

        var rentResponse = await client.PostAsJsonAsync("/rentals",
            RentalRequestFor(await testWebApplication.SeedAvailableVehicleAsync()));
        rentResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reserveResponse = await client.PostAsJsonAsync("/reservations",
            ReservationRequestFor(await testWebApplication.SeedAvailableVehicleAsync()));
        reserveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenChargeDeclined_SettlePayment_ShouldReturnBadRequestAndAllowALaterSuccessfulAttempt()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var rentalId = await testWebApplication.CreateUnpaidRentalAsync(
            client, await testWebApplication.SeedAvailableVehicleAsync());

        testWebApplication.PaymentGateway.ChargeResult = ChargeResult.Declined;

        // Act
        var declinedResponse = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        declinedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await declinedResponse.Content.ReadFromJsonAsync<string>()).ShouldBe("Payment declined.");

        var rentalAfterDecline = await testWebApplication.GetRentalAsync(rentalId);
        rentalAfterDecline.PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
        rentalAfterDecline.PaidAt.ShouldBeNull();
        testWebApplication.PaymentGateway.Charges.ShouldBeEmpty();

        // Act: the customer tops up the wallet and retries.
        testWebApplication.PaymentGateway.ChargeResult = ChargeResult.Succeeded;

        var retriedResponse = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        retriedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var rental = await testWebApplication.GetRentalAsync(rentalId);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        rental.PaidAt.ShouldNotBeNull();
        testWebApplication.PaymentGateway.Charges.ShouldHaveSingleItem().IdempotencyKey.ShouldBe(rentalId);
    }

    [Fact]
    public async Task GivenRentalIsAlreadyPaid_SettlePayment_ShouldReturnBadRequestWithoutCallingTheGateway()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var rentalId = await CreateRentalAsync(client, vehicleId);
        (await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object())).EnsureSuccessStatusCode();

        var chargeAttempts = 0;
        testWebApplication.PaymentGateway.OnCharge = () =>
        {
            Interlocked.Increment(ref chargeAttempts);
            return Task.CompletedTask;
        };

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe("Rental has no outstanding payment.");

        chargeAttempts.ShouldBe(0);
        testWebApplication.PaymentGateway.Charges.ShouldHaveSingleItem();
        (await testWebApplication.GetRentalAsync(rentalId)).PaymentStatus.ShouldBe(PaymentStatus.Paid);
    }

    [Fact]
    public async Task GivenRentalIsStillActive_SettlePayment_ShouldReturnNotFound()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var rentalId = await CreateRentalAsync(client, await testWebApplication.SeedAvailableVehicleAsync());

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe($"Rental with ID {rentalId} does not exist.");

        var rental = await testWebApplication.GetRentalAsync(rentalId);
        rental.Status.ShouldBe(RentalStatus.Active);
        testWebApplication.PaymentGateway.Charges.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenRentalOfAnotherCustomer_SettlePayment_ShouldReturnNotFound()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var otherCustomer = await testWebApplication.CreateClient().SignInAsAdminAsync(testWebApplication);
        var rentalId = await testWebApplication.CreateUnpaidRentalAsync(
            owner, await testWebApplication.SeedAvailableVehicleAsync());

        // Act
        var response = await otherCustomer.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe($"Rental with ID {rentalId} does not exist.");

        (await testWebApplication.GetRentalAsync(rentalId)).PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
        testWebApplication.PaymentGateway.Charges.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenUnknownRentalId_SettlePayment_ShouldReturnNotFound()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var rentalId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe($"Rental with ID {rentalId} does not exist.");
    }

    [Fact]
    public async Task GivenGatewayIsUnavailable_SettlePayment_ShouldReturn503AndKeepTheRentalUnpaid()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var rentalId = await testWebApplication.CreateUnpaidRentalAsync(
            client, await testWebApplication.SeedAvailableVehicleAsync());

        testWebApplication.PaymentGateway.IsUnavailable = true;

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadFromJsonAsync<string>()).ShouldBe("Payment gateway is unavailable.");

        var rental = await testWebApplication.GetRentalAsync(rentalId);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
        rental.PaidAt.ShouldBeNull();
        testWebApplication.PaymentGateway.Charges.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenUserIsNotSignedIn_SettlePayment_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = testWebApplication.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{Guid.NewGuid()}/settle", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<Guid> CreateRentalAsync(HttpClient client, Guid vehicleId)
    {
        var response = await client.PostAsJsonAsync("/rentals", RentalRequestFor(vehicleId));
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static CreateRentalEndpoint.Request RentalRequestFor(Guid vehicleId)
    {
        var startDate = DateTimeOffset.UtcNow.AddMinutes(3);

        return new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = startDate,
            EndDate = startDate.AddHours(2)
        };
    }

    private static CreateReservationEndpoint.Request ReservationRequestFor(Guid vehicleId)
    {
        return new CreateReservationEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = DateTimeOffset.UtcNow.AddMinutes(3),
            DurationInSeconds = 3600
        };
    }
}
