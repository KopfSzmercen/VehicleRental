using Shouldly;
using VehicleRental.Vehicles.Domain;

namespace VehicleTests.Tests.Unit.Vehicles.Domain;

public class GeoLocalizationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(45.5, -73.6)]
    public void Create_WithValidCoordinates_ShouldSucceed(double latitude, double longitude)
    {
        // Act
        var geoLocalization = GeoLocalization.Create(latitude, longitude);

        // Assert
        geoLocalization.ShouldNotBeNull();
        geoLocalization.Latitude.ShouldBe(latitude);
        geoLocalization.Longitude.ShouldBe(longitude);
    }

    [Theory]
    [InlineData(90.1, 0)]
    [InlineData(-90.1, 0)]
    public void Create_WithInvalidLatitude_ShouldThrowArgumentOutOfRangeException(double latitude, double longitude)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => GeoLocalization.Create(latitude, longitude))
            .ParamName.ShouldBe("latitude");
    }

    [Theory]
    [InlineData(0, 180.1)]
    [InlineData(0, -180.1)]
    public void Create_WithInvalidLongitude_ShouldThrowArgumentOutOfRangeException(double latitude, double longitude)
    {
        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => GeoLocalization.Create(latitude, longitude))
            .ParamName.ShouldBe("longitude");
    }
}
