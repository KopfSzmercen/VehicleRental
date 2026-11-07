namespace VehicleRental.Rentals.Domain;

// Payment state of a completed rental. Null while the rental is Active or Cancelled.
public enum PaymentStatus
{
    Paid,
    Unpaid
}
