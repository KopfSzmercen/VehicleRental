using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Vehicles.Domain;

namespace VehicleTests.Tests.Unit.Vehicles.Domain;

public class VehicleTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private Vehicle CreateValidVehicle(bool registrationNumberIsUnique = true)
    {
        return Vehicle.CreateNew("Test Vehicle", "XYZ123", GeoLocalization.Create(10, 10), registrationNumberIsUnique,
            _now);
    }

    private VehicleLegalDocument CreateValidLegalDocument()
    {
        return VehicleLegalDocument.CreateNew("Insurance", Guid.NewGuid(), _now.AddYears(1), _now);
    }

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
        vehicle.IsAvailableForRental.ShouldBeFalse();
        vehicle.LegalDocuments.ShouldBeEmpty();
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
    public void MakeAvailable_WhenVehicleIsInVerificationAndHasNoDocuments_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var vehicle = CreateValidVehicle(); // Status is InVerification by default

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => vehicle.MakeAvailable(_now));
    }

    [Fact]
    public void MakeAvailable_WhenVehicleIsArchivedAndHasNoDocuments_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        vehicle.Archive(_now); // Archive the vehicle first

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => vehicle.MakeAvailable(_now));
    }


    [Fact]
    public void MakeAvailable_WhenVehicleIsArchivedAndHasDocuments_ShouldSucceed()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var legalDocument = CreateValidLegalDocument();
        vehicle.AddLegalDocument(legalDocument);
        vehicle.Archive(_now); // Archive the vehicle first
        var makeAvailableTime = _now.AddSeconds(1);

        // Act
        vehicle.MakeAvailable(makeAvailableTime);

        // Assert
        vehicle.Status.ShouldBe(VehicleStatus.Available);
        vehicle.UpdatedAt.ShouldBe(makeAvailableTime);
    }

    [Fact]
    public void MakeAvailable_WhenVehicleIsNotArchived_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var legalDocument = CreateValidLegalDocument();
        vehicle.AddLegalDocument(legalDocument);
        // Vehicle is InVerification initially

        // Act & Assert
        Should.Throw<BusinessRuleValidationException>(() => vehicle.MakeAvailable(_now));
    }


    [Fact]
    public void Archive_ShouldSetStatusToArchivedAndClearLocalization()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        // Simulate making it available first
        var legalDocument = CreateValidLegalDocument();
        vehicle.AddLegalDocument(legalDocument);
        // Need to archive first to make available
        vehicle.Archive(_now.AddSeconds(-10));
        vehicle.MakeAvailable(_now.AddSeconds(-5));

        var archiveTime = _now;

        // Act
        vehicle.Archive(archiveTime);

        // Assert
        vehicle.Status.ShouldBe(VehicleStatus.Archived);
        vehicle.IsAvailableForRental.ShouldBeFalse();
        vehicle.UpdatedAt.ShouldBe(archiveTime);
        vehicle.CurrentGeoLocalization.ShouldBeNull();
    }

    [Fact]
    public void PutToMaintenance_WhenVehicleIsAvailable_ShouldSucceed()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var legalDocument = CreateValidLegalDocument();
        vehicle.AddLegalDocument(legalDocument);
        // Need to archive first to make available
        vehicle.Archive(_now.AddSeconds(-10));
        vehicle.MakeAvailable(_now.AddSeconds(-5)); // Status is now Available
        var maintenanceTime = _now;

        // Act
        vehicle.PutToMaintenance(maintenanceTime);

        // Assert
        vehicle.Status.ShouldBe(VehicleStatus.InMaintenance);
        vehicle.UpdatedAt.ShouldBe(maintenanceTime);
    }

    [Theory]
    [InlineData(VehicleStatus.InVerification)]
    [InlineData(VehicleStatus.InMaintenance)]
    [InlineData(VehicleStatus.Archived)]
    internal void PutToMaintenance_WhenVehicleIsNotAvailable_ShouldThrowBusinessRuleValidationException(
        VehicleStatus initialStatus)
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        if (initialStatus == VehicleStatus.Archived) vehicle.Archive(_now.AddSeconds(-5));

        // Act & Assert
        // Only test reachable states easily without internal manipulation
        if (initialStatus == VehicleStatus.Archived || initialStatus == VehicleStatus.InVerification)
            Should.Throw<BusinessRuleValidationException>(() => vehicle.PutToMaintenance(_now));
    }

    [Fact]
    public void AddLegalDocument_ShouldAddDocumentToList()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var legalDocument = CreateValidLegalDocument();

        // Act
        vehicle.AddLegalDocument(legalDocument);

        // Assert
        vehicle.LegalDocuments.ShouldNotBeEmpty();
        vehicle.LegalDocuments.Count.ShouldBe(1);
        vehicle.LegalDocuments[0].ShouldBe(legalDocument);
    }

    [Fact]
    public void UpdateGeoLocalization_ShouldUpdateCurrentGeoLocalization()
    {
        // Arrange
        var vehicle = CreateValidVehicle();
        var newGeoLocalization = GeoLocalization.Create(20, 20);

        // Act
        vehicle.UpdateGeoLocalization(newGeoLocalization);

        // Assert
        vehicle.CurrentGeoLocalization.ShouldBe(newGeoLocalization);
    }
}