namespace VehicleRental.Common.Endpoints;

internal interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}