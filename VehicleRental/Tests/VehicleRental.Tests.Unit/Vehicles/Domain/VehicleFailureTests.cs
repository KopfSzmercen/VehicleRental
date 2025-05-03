using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Vehicles.Domain;

namespace VehicleTests.Tests.Unit.Vehicles.Domain;

public class VehicleFailureTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly Guid _vehicleId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();

    private VehicleFailure CreateValidVehicleFailure() =>
        VehicleFailure.CreateNew("Engine Failure", _vehicleId, _creatorId, _now);

    [Fact]
    public void CreateNew_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string name = "Engine Failure";

        // Act
        var failure = VehicleFailure.CreateNew(name, _vehicleId, _creatorId, _now);

        // Assert
        failure.ShouldNotBeNull();
        failure.Id.ShouldNotBe(Guid.Empty);
        failure.Name.ShouldBe(name);
        failure.VehicleId.ShouldBe(_vehicleId);
        failure.CreatorId.ShouldBe(_creatorId);
        failure.CreatedAt.ShouldBe(_now);
        failure.Status.ShouldBe(VehicleFailureStatus.Open);
        failure.UpdatedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateNew_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
                VehicleFailure.CreateNew(name, _vehicleId, _creatorId, _now))
            .ParamName.ShouldBe("name");
    }

    [Fact]
    public void Resolve_WhenStatusIsOpen_ShouldSucceed()
    {
        // Arrange
        var failure = CreateValidVehicleFailure(); // Status is Open by default
        var resolveTime = _now.AddSeconds(1);

        // Act
        failure.Resolve(resolveTime);

        // Assert
        failure.Status.ShouldBe(VehicleFailureStatus.Resolved);
        failure.UpdatedAt.ShouldBe(resolveTime);
    }

    [Fact]
    public void Resolve_WhenStatusIsNotOpen_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var failure = CreateValidVehicleFailure();
        var resolveTime = _now.AddSeconds(1);
        failure.Resolve(resolveTime); // Status is now Resolved

        var secondResolveTime = _now.AddSeconds(2);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => failure.Resolve(secondResolveTime));
    }

    [Fact]
    public void CanNotBeResolved_WhenStatusIsOpen_ShouldSucceed()
    {
        // Arrange
        var failure = CreateValidVehicleFailure(); // Status is Open by default
        var markTime = _now.AddSeconds(1);

        // Act
        failure.CanNotBeResolved(markTime);

        // Assert
        failure.Status.ShouldBe(VehicleFailureStatus.CanNotBeResolved);
        failure.UpdatedAt.ShouldBe(markTime);
    }

    [Fact]
    public void CanNotBeResolved_WhenStatusIsNotOpen_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var failure = CreateValidVehicleFailure();
        var resolveTime = _now.AddSeconds(1);
        failure.Resolve(resolveTime); // Status is now Resolved

        var markTime = _now.AddSeconds(2);

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => failure.CanNotBeResolved(markTime));
    }
}
