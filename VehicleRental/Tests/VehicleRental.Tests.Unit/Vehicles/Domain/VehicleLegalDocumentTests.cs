using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Vehicles.Domain.Vehicles;

namespace VehicleTests.Tests.Unit.Vehicles.Domain;

public class VehicleLegalDocumentTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateNew_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string name = "Insurance";
        var vehicleId = Guid.NewGuid();
        var validTo = _now.AddYears(1);

        // Act
        var document = VehicleLegalDocument.CreateNew(name, vehicleId, validTo, _now);

        // Assert
        document.ShouldNotBeNull();
        document.Id.ShouldNotBe(Guid.Empty);
        document.Name.ShouldBe(name);
        document.VehicleId.ShouldBe(vehicleId);
        document.ValidTo.ShouldBe(validTo);
        document.CreatedAt.ShouldBe(_now);
        document.UpdatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateNew_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var validTo = _now.AddYears(1);

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
                VehicleLegalDocument.CreateNew(name, vehicleId, validTo, _now))
            .ParamName.ShouldBe("name");
    }

    [Fact]
    public void CreateNew_WithValidToInThePast_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        const string name = "Insurance";
        var vehicleId = Guid.NewGuid();
        var validTo = _now.AddDays(-1);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() =>
            VehicleLegalDocument.CreateNew(name, vehicleId, validTo, _now));
    }
}