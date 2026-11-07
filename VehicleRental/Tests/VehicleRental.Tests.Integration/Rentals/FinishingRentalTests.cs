using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
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
}