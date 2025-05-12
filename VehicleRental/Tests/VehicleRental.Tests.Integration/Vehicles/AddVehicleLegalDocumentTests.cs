using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VehicleRental.Vehicles.Endpoints;

namespace VehicleRental.Tests.Integration.Vehicles;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class AddVehicleLegalDocumentTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenValidRequestAndAdminUser_AddLegalDocument_ShouldSucceed()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        // First, create a vehicle to add the document to
        var createVehicleRequest = new CreateVehicleEndpoint.Request
        {
            Name = "Test Vehicle for Document",
            RegistrationNumber = "DOC123",
            GeoLocalization = new CreateVehicleEndpoint.GeoLocalizationRequest
            {
                Latitude = 10,
                Longitude = 10
            }
        };
        var createResponse = await client.PostAsJsonAsync("/vehicles", createVehicleRequest);
        createResponse.EnsureSuccessStatusCode();
        var vehicleId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var addDocumentRequest = new AddVehicleLegalDocumentEndpoint.Request
        {
            Name = "Insurance Policy",
            ValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PostAsJsonAsync($"/vehicles/{vehicleId}/legal-documents", addDocumentRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var documentId = await response.Content.ReadFromJsonAsync<Guid>();
        documentId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task GivenInvalidVehicleId_AddLegalDocument_ShouldReturnNotFound()
    {
        // Arrange
        var client = await testWebApplication
            .CreateClient()
            .SignInAsAdminAsync(testWebApplication);

        var invalidVehicleId = Guid.NewGuid();
        var addDocumentRequest = new AddVehicleLegalDocumentEndpoint.Request
        {
            Name = "Insurance Policy",
            ValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        // Act
        var response =
            await client.PostAsJsonAsync($"/vehicles/{invalidVehicleId}/legal-documents", addDocumentRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}