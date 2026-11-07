using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Vehicles;

namespace VehicleRental.Tests.Integration.Vehicles;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class VehiclesModuleApiTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }

    [Fact]
    public async Task GivenAvailableVehicle_IsAvailableForRental_ShouldReturnTrue()
    {
        var vehicleId = await testWebApplication.SeedAvailableVehicleAsync();

        var isAvailable = await IsAvailableForRentalAsync(vehicleId);

        isAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenVehicleInVerification_IsAvailableForRental_ShouldReturnFalse()
    {
        var vehicleId = await testWebApplication.SeedVehicleInVerificationAsync();

        var isAvailable = await IsAvailableForRentalAsync(vehicleId);

        isAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenUnknownVehicle_IsAvailableForRental_ShouldReturnFalse()
    {
        var isAvailable = await IsAvailableForRentalAsync(Guid.NewGuid());

        isAvailable.ShouldBeFalse();
    }

    private async Task<bool> IsAvailableForRentalAsync(Guid vehicleId)
    {
        await using var scope = testWebApplication.Services.CreateAsyncScope();
        var vehiclesModuleApi = scope.ServiceProvider.GetRequiredService<IVehiclesModuleApi>();

        return await vehiclesModuleApi.IsAvailableForRentalAsync(vehicleId);
    }
}
