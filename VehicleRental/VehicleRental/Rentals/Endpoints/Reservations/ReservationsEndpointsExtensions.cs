using VehicleRental.Common.Endpoints;

namespace VehicleRental.Rentals.Endpoints.Reservations;

public static class ReservationsEndpointsExtensions
{
    public const string BaseUrl = "reservations";
    public const string GroupTag = "reservations";

    public static void AddReservationsEndpoints(this WebApplication webApplication)
    {
        var group = webApplication
            .MapGroup(BaseUrl)
            .WithTags(GroupTag);

        group
            .MapEndpoint<CreateReservationEndpoint>();
    }
}