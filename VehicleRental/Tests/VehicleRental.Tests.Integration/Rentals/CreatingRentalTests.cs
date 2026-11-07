using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Endpoints;

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
