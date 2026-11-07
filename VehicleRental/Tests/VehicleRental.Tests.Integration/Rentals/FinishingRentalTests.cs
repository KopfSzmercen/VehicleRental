using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;
using VehicleRental.Rentals.Domain.Payments;
using VehicleRental.Rentals.Endpoints;

namespace VehicleRental.Tests.Integration.Rentals;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class FinishingRentalTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenValidRequestAndUserIsSignedIn_FinishRental_ShouldSucceed()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();

        var createRentalRequest = new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = DateTimeOffset.Now.AddMinutes(3),
            EndDate = DateTimeOffset.Now.AddDays(1)
        };

        var response = await client.PostAsJsonAsync("/rentals", createRentalRequest);
        response.EnsureSuccessStatusCode();

        var rentalId = await response.Content.ReadFromJsonAsync<Guid>();

        // Act
        response = await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using (var scope = testWebApplication.Services.CreateAsyncScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            var rentalVehicle = await dbContext.RentalVehicles
                .Include(rv => rv.Rental)
                .SingleAsync(rv => rv.Id == vehicleId);

            rentalVehicle.ShouldNotBeNull();
            rentalVehicle.Rental.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GivenChargeSucceeds_FinishRental_ShouldCompleteAsPaidAndChargeThePriceOnce()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var customerId = await testWebApplication.GetUserIdAsync("user@t.pl");
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var rentalId = await CreateTwoHourRentalAsync(client, vehicleId);

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var rental = await GetRentalAsync(rentalId);
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        rental.PaidAt.ShouldNotBeNull();

        var charge = testWebApplication.PaymentGateway.Charges.ShouldHaveSingleItem();
        charge.CustomerId.ShouldBe(customerId);
        charge.Amount.ShouldBe(new Money(20, Currency.USD));
        charge.IdempotencyKey.ShouldBe(rentalId);
    }

    [Fact]
    public async Task GivenChargeDeclined_FinishRental_ShouldCompleteAsUnpaidAndFreeTheVehicle()
    {
        // Arrange
        var owner = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var otherClient = await testWebApplication.CreateClient().SignInAsAdminAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var rentalId = await CreateTwoHourRentalAsync(owner, vehicleId);

        testWebApplication.PaymentGateway.ChargeResult = ChargeResult.Declined;

        // Act
        var response = await owner.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var rental = await GetRentalAsync(rentalId);
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
        rental.PaidAt.ShouldBeNull();
        testWebApplication.PaymentGateway.Charges.ShouldBeEmpty();

        await CreateTwoHourRentalAsync(otherClient, vehicleId);
    }

    [Fact]
    public async Task GivenGatewayIsUnavailable_FinishRental_ShouldReturn503AndKeepTheRentalActive()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var rentalId = await CreateTwoHourRentalAsync(client, vehicleId);

        testWebApplication.PaymentGateway.IsUnavailable = true;

        // Act
        var response = await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Payment gateway is unavailable.");

        var rental = await GetRentalAsync(rentalId);
        rental.Status.ShouldBe(RentalStatus.Active);
        rental.PaymentStatus.ShouldBeNull();
        rental.CurrentVehicleId.ShouldBe(vehicleId);
        testWebApplication.PaymentGateway.Charges.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenSaveFailsAfterSuccessfulCharge_RetriedFinishRental_ShouldCompleteAsPaidWithoutChargingTwice()
    {
        // Arrange
        var client = await testWebApplication.CreateClient().SignInAsUserAsync(testWebApplication);
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();
        var rentalId = await CreateTwoHourRentalAsync(client, vehicleId);

        // While the gateway is charging, someone else changes the vehicle row, so the xmin check fails on save.
        testWebApplication.PaymentGateway.OnCharge = async () =>
        {
            await using var scope = testWebApplication.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.RentalVehicles
                .Where(rv => rv.Id == vehicleId)
                .ExecuteUpdateAsync(u => u.SetProperty(rv => rv.UpdatedAt, DateTimeOffset.UtcNow));
        };

        // Act
        var failedResponse = await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());

        // Assert
        failedResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await failedResponse.Content.ReadAsStringAsync()).ShouldContain(
            "The data you are trying to update has been modified by another user. Please reload the data and try again.");

        var rentalAfterFailedSave = await GetRentalAsync(rentalId);
        rentalAfterFailedSave.Status.ShouldBe(RentalStatus.Active);
        rentalAfterFailedSave.PaymentStatus.ShouldBeNull();

        // Act: the customer retries.
        testWebApplication.PaymentGateway.OnCharge = null;

        var retriedResponse = await client.PutAsJsonAsync($"/rentals/{rentalId}/finish", new object());

        // Assert
        retriedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var rental = await GetRentalAsync(rentalId);
        rental.Status.ShouldBe(RentalStatus.Completed);
        rental.PaymentStatus.ShouldBe(PaymentStatus.Paid);

        var charge = testWebApplication.PaymentGateway.Charges.ShouldHaveSingleItem();
        charge.IdempotencyKey.ShouldBe(rentalId);
    }

    // A rental of exactly 2 hours, so its price is 20 USD.
    private static async Task<Guid> CreateTwoHourRentalAsync(HttpClient client, Guid vehicleId)
    {
        var startDate = DateTimeOffset.UtcNow.AddMinutes(3);

        var response = await client.PostAsJsonAsync("/rentals", new CreateRentalEndpoint.Request
        {
            VehicleId = vehicleId,
            StartDate = startDate,
            EndDate = startDate.AddHours(2)
        });
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private async Task<Rental> GetRentalAsync(Guid rentalId)
    {
        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Set<Rental>()
            .AsNoTracking()
            .SingleAsync(r => r.Id == rentalId);
    }
}
