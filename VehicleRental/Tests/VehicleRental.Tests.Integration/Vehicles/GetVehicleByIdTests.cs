using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Vehicles.Endpoints;

namespace VehicleRental.Tests.Integration.Vehicles;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class GetVehicleByIdTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenVehicleExists_GetVehicleById_ShouldSucceedAndReturnOk()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var createVehicleRequest = new CreateVehicleEndpoint.Request
        {
            Name = "Test Vehicle",
            RegistrationNumber = "TEST123",
            GeoLocalization = new CreateVehicleEndpoint.GeoLocalizationRequest
            {
                Latitude = 50.0,
                Longitude = 19.0
            }
        };

        var createResponse = await client.PostAsJsonAsync(VehiclesEndpointsExtensions.BaseUrl, createVehicleRequest);
        var vehicleId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Act
        var getResponse = await client.GetAsync($"{VehiclesEndpointsExtensions.BaseUrl}/{vehicleId}");

        // Assert
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var vehicleDto = await getResponse.Content.ReadFromJsonAsync<GetVehicleEndpoint.VehicleDto>();
        vehicleDto.ShouldNotBeNull();
    }
}