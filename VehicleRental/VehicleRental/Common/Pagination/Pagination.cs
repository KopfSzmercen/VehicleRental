using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VehicleRental.Common.Pagination;

public interface IPaginatedEntity<out T>
{
    IEnumerable<T> Items { get; }

    int TotalCount { get; }

    int PageNumber { get; }

    int PageSize { get; }
}

public interface IEntityWithId
{
    Guid Id { get; }
}

public class PaginatedEntity<T>(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    : IPaginatedEntity<T>
{
    public IEnumerable<T> Items { get; } = items;

    public int TotalCount { get; } = totalCount;

    public int PageNumber { get; } = pageNumber;

    public int PageSize { get; } = pageSize;
}

public sealed record PaginationQuery
{
    [FromQuery] public int PageNumber { get; init; } = 1;

    [FromQuery] public int PageSize { get; init; } = 10;
}

public static class Pagination
{
    public static async Task<IPaginatedEntity<T>> PaginateAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        where T : class, IEntityWithId
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");

        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return new PaginatedEntity<T>(items, totalCount, pageNumber, pageSize);
    }
}