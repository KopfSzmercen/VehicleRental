namespace VehicleRental.Vehicles.Domain;

internal sealed record GeoLocalization
{
    private GeoLocalization()
    {
    }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public static GeoLocalization Create(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees.");

        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees.");

        return new GeoLocalization
        {
            Latitude = latitude,
            Longitude = longitude
        };
    }
}