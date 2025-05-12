using VehicleRental.Common.Endpoints;

namespace VehicleRental.Vehicles.Endpoints;

internal static class VehiclesEndpointsExtensions
{
    public const string BaseUrl = "vehicles";
    public const string GroupTag = "vehicles";

    public static void AddVehiclesEndpoints(this WebApplication webApplication)
    {
        var group = webApplication
            .MapGroup(BaseUrl)
            .WithTags(GroupTag);

        group
            .MapEndpoint<CreateVehicleEndpoint>()
            .MapEndpoint<AddVehicleLegalDocumentEndpoint>()
            .MapEndpoint<MakeVehicleAvailableEndpoint>()
            .MapEndpoint<BrowseVehiclesEndpoint>();
    }
}