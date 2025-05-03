namespace VehicleRental.Vehicles.Domain;

internal enum VehicleStatus
{
    Available = 1,
    Rented = 2,
    InMaintenance = 3,
    Reserved = 4,
    OutOfService = 5,
    InVerification = 6
}