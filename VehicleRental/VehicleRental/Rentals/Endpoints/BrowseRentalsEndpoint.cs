using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using VehicleRental.Common.Endpoints;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;
using VehicleRental.Users.Domain;

namespace VehicleRental.Rentals.Endpoints;

public sealed class BrowseRentalsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithSummary("Browse rentals")
            .RequireAuthorization()
            .WithRequestValidation<Request>();
    }

    private static async Task<Results<
            Ok<IPaginatedEntity<BrowseRentalItemResponse>>,
            BadRequest<string>>
    > Handle(
        [AsParameters] Request request,
        [AsParameters] PaginationQuery paginationQuery,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] AppReadDbContext dbContext
    )
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirst("UserId")!.Value!;

        var userRole = httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value!;

        if (userRole == UserRole.User)
            return await HandleNormalUserRequest(request, paginationQuery, Guid.Parse(userIdString), dbContext);

        return await HandleAdminRequest(request, paginationQuery, dbContext);
    }

    private static async Task<Ok<IPaginatedEntity<BrowseRentalItemResponse>>> HandleAdminRequest(
        Request request,
        PaginationQuery paginationQuery,
        AppReadDbContext dbContext
    )
    {
        var rentalsQueryable = dbContext.Rentals.AsQueryable();

        if (request.UsersIds is not null && request.UsersIds.Length != 0)
            rentalsQueryable = rentalsQueryable.Where(r => request.UsersIds.Contains(r.CustomerId));

        if (request.VehiclesIds is not null && request.VehiclesIds.Length != 0)
            rentalsQueryable = rentalsQueryable.Where(r => request.VehiclesIds.Contains(r.VehicleId));

        var rentals = await rentalsQueryable.Select(r => new BrowseRentalItemResponse
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                UserId = r.CustomerId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                CancelledAt = r.CancelledAt,
                CreatedAt = r.CreatedAt,
                CompletedAt = r.CompletedAt
            })
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        return TypedResults.Ok(rentals);
    }

    private static async Task<Ok<IPaginatedEntity<BrowseRentalItemResponse>>> HandleNormalUserRequest(
        Request request,
        PaginationQuery paginationQuery,
        Guid userId,
        AppReadDbContext dbContext
    )
    {
        var rentalsQueryable = dbContext.Rentals
            .Where(r => r.CustomerId == userId);

        if (request.VehiclesIds is not null && request.VehiclesIds.Length != 0)
            rentalsQueryable = rentalsQueryable.Where(r => request.VehiclesIds.Contains(r.VehicleId));

        var rentals = await rentalsQueryable.Select(r => new BrowseRentalItemResponse
            {
                Id = r.Id,
                VehicleId = r.VehicleId,
                UserId = r.CustomerId,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                CancelledAt = r.CancelledAt,
                CreatedAt = r.CreatedAt,
                CompletedAt = r.CompletedAt
            })
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        return TypedResults.Ok(rentals);
    }

    public sealed record BrowseRentalItemResponse : IEntityWithId
    {
        public Guid VehicleId { get; init; }
        public Guid UserId { get; init; }
        public DateTimeOffset StartDate { get; init; }
        public DateTimeOffset EndDate { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? CompletedAt { get; init; }

        public DateTimeOffset? CancelledAt { get; init; }

        public Guid Id { get; init; }
    }

    public sealed record Request
    {
        [FromQuery] public Guid[]? VehiclesIds { get; set; }
        [FromQuery] public Guid[]? UsersIds { get; set; }
    }
}