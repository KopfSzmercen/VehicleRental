using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Vehicles.Endpoints;

namespace VehicleRental.Tests.Integration.Vehicles;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class CreateVehicleTests(TestWebApplication testWebApplication) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await testWebApplication.ResetDatabaseAsync();
    }

    [Fact]
    public async Task GivenValidRequestAndAdminUser_CreateVehicle_ShouldSucceed()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var createVehicleRequest = new CreateVehicleEndpoint.Request
        {
            Name = "Test Car",
            RegistrationNumber = "TEST1234",
            GeoLocalization = new CreateVehicleEndpoint.GeoLocalizationRequest
            {
                Latitude = 40.7128,
                Longitude = -74.0060
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/vehicles", createVehicleRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var vehicleId = await response.Content.ReadFromJsonAsync<Guid>();
        vehicleId.ShouldNotBe(Guid.Empty);
    }
}