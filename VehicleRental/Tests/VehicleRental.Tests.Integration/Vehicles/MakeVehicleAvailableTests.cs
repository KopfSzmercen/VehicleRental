using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Vehicles.Endpoints;

namespace VehicleRental.Tests.Integration.Vehicles;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class MakeVehicleAvailableTests(TestWebApplication testWebApplication) : IDisposable
{
    private readonly TestWebApplication _testWebApplication = testWebApplication;

    public void Dispose()
    {
        _testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenValidRequestAndAdminUser_MakeVehicleAvailable_ShouldSucceedAndReturnOk()
    {
        // Arrange
        var client = await _testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(_testWebApplication);

        var createVehicleRequest = new CreateVehicleEndpoint.Request
        {
            Name = "Test Vehicle To Make Available",
            RegistrationNumber = "AVAIL123",
            GeoLocalization = new CreateVehicleEndpoint.GeoLocalizationRequest
            {
                Latitude = 50.0,
                Longitude = 19.0
            }
        };

        var createResponse = await client.PostAsJsonAsync(VehiclesEndpointsExtensions.BaseUrl, createVehicleRequest);
        var vehicleId = await createResponse.Content.ReadFromJsonAsync<Guid>();


        var addDocumentRequest = new AddVehicleLegalDocumentEndpoint.Request
        {
            Name = "Insurance",
            ValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        await client.PostAsJsonAsync($"{VehiclesEndpointsExtensions.BaseUrl}/{vehicleId}/legal-documents",
            addDocumentRequest);

        // Act
        var makeAvailableResponse =
            await client.PutAsync($"{VehiclesEndpointsExtensions.BaseUrl}/{vehicleId}/make-available", null);

        // Assert
        makeAvailableResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}