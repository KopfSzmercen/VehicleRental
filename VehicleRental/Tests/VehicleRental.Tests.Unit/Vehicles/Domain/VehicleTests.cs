using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Vehicles.Domain;

namespace VehicleTests.Tests.Unit.Vehicles.Domain;

public class VehicleTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateNew_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string name = "Test Vehicle";
        const string registrationNumber = "XYZ123";
        var geoLocalization = GeoLocalization.Create(10, 10);
        const bool registrationNumberIsUnique = true;

        // Act
        var vehicle = Vehicle.CreateNew(name, registrationNumber, geoLocalization, registrationNumberIsUnique, _now);

        // Assert
        vehicle.ShouldNotBeNull();
        vehicle.Id.ShouldNotBe(Guid.Empty);
        vehicle.Name.ShouldBe(name);
        vehicle.RegistrationNumber.ShouldBe(registrationNumber);
        vehicle.Status.ShouldBe(VehicleStatus.InVerification);
        vehicle.CurrentGeoLocalization.ShouldBe(geoLocalization);
        vehicle.CreatedAt.ShouldBe(_now);
        vehicle.UpdatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateNew_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        const string registrationNumber = "XYZ123";
        var geoLocalization = GeoLocalization.Create(10, 10);
        const bool registrationNumberIsUnique = true;

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
                Vehicle.CreateNew(name, registrationNumber, geoLocalization, registrationNumberIsUnique, _now))
            .ParamName.ShouldBe("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateNew_WithInvalidRegistrationNumber_ShouldThrowArgumentException(string registrationNumber)
    {
        // Arrange
        const string name = "Test Vehicle";
        var geoLocalization = GeoLocalization.Create(10, 10);
        const bool registrationNumberIsUnique = true;

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
                Vehicle.CreateNew(name, registrationNumber, geoLocalization, registrationNumberIsUnique, _now))
            .ParamName.ShouldBe("registrationNumber");
    }

    [Fact]
    public void CreateNew_WhenRegistrationNumberIsNotUnique_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        const string name = "Test Vehicle";
        const string registrationNumber = "XYZ123";
        var geoLocalization = GeoLocalization.Create(10, 10);
        const bool registrationNumberIsUnique = false;

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
            Vehicle.CreateNew(name, registrationNumber, geoLocalization, registrationNumberIsUnique, _now));
    }

    [Fact]
    public void UpdateGeoLocalization_ShouldUpdateCurrentGeoLocalization()
    {
        // Arrange
        var vehicle = Vehicle.CreateNew("Test", "REG123", GeoLocalization.Create(10, 10), true, _now);
        var newGeoLocalization = GeoLocalization.Create(20, 20);

        // Act
        vehicle.UpdateGeoLocalization(newGeoLocalization);

        // Assert
        vehicle.CurrentGeoLocalization.ShouldBe(newGeoLocalization);
    }
}
