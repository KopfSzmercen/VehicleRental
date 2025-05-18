using VehicleRental.Common.Endpoints;

namespace VehicleRental.Rentals.Endpoints;

internal static class RentalsEndpointsExtensions
{
    public const string BaseUrl = "rentals";
    public const string GroupTag = "rentals";

    public static void AddRentalsEndpoints(this WebApplication webApplication)
    {
        var group = webApplication
            .MapGroup(BaseUrl)
            .WithTags(GroupTag);

        group
            .MapEndpoint<CreateRentalEndpoint>()
            .MapEndpoint<FinishRentalEndpoint>()
            .MapEndpoint<BrowseRentalsEndpoint>();
    }
}