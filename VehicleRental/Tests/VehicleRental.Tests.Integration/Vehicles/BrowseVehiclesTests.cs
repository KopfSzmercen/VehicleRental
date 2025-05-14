using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Common.Pagination;
using VehicleRental.Vehicles.Endpoints;

namespace VehicleRental.Tests.Integration.Vehicles;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class BrowseVehiclesTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenValidRequestAndAuthorizedUser_BrowseVehicles_ShouldSucceed()
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
        await client.PostAsJsonAsync("/vehicles", createVehicleRequest);

        // Act
        var response = await client.GetAsync("/vehicles?PageNumber=1&PageSize=10");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var deserializedResponse = await response.Content
            .ReadFromJsonAsync<PaginatedEntity<BrowseVehiclesEndpoint.BrowseVehiclesItemDto>>();

        deserializedResponse.ShouldNotBeNull();
        deserializedResponse.ShouldBeOfType<PaginatedEntity<BrowseVehiclesEndpoint.BrowseVehiclesItemDto>>();
    }
}