using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Endpoints;
using VehicleRental.Vehicles.Domain;
using VehicleRental.Vehicles.Domain.Vehicles;

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

        var vehicle = Vehicle.CreateNew(
            "Test Car",
            "TEST1234",
            GeoLocalization.Create(40.7128, -74.0060),
            true,
            DateTimeOffset.UtcNow
        );

        using (var scope = testWebApplication.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            dbContext.Vehicles.Add(vehicle);
            await dbContext.SaveChangesAsync();
        }

        var createRentalRequest = new CreateRentalEndpoint.Request
        {
            VehicleId = vehicle.Id,
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
    }
}