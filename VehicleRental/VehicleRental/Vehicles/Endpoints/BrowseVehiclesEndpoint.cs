using Microsoft.EntityFrameworkCore;
using VehicleRental.Common.Endpoints;
using VehicleRental.Persistence;

namespace VehicleRental.Vehicles.Endpoints;

internal sealed class BrowseVehiclesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithSummary("Browse all vehicles")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        AppReadDbContext dbContext
    )
    {
        var vehicles = await dbContext.Vehicles
            .AsNoTracking()
            .ToListAsync();

        return Results.Ok(vehicles);
    }
}