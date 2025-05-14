using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Common.Pagination;
using VehicleRental.Persistence;
using VehicleRental.Vehicles.Domain;
using VehicleRental.Vehicles.Infrastructure.ReadModels;

namespace VehicleRental.Tests.Integration;

public class PaginationTests(TestWebApplication factory)
    : IClassFixture<TestWebApplication>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        var serviceScopeProvider = factory.Services.GetRequiredService<IServiceScopeFactory>();

        await using var scope = serviceScopeProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppReadDbContext>();

        // Seed the database with test data
        var vehicles = new List<VehicleReadModel>();

        for (var i = 0; i < 100; i++)
            vehicles.Add(new VehicleReadModel
            {
                Id = Guid.NewGuid(),
                Name = $"Vehicle {i}",
                RegistrationNumber = $"REG-{i}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                CurrentGeoLocalization = GeoLocalization.Create(0, 0)
            });

        await dbContext.Vehicles.AddRangeAsync(vehicles);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    [Fact]
    public async Task PaginationShouldReturnPaginatedResults()
    {
        // Arrange
        var serviceScopeProvider = factory.Services.GetRequiredService<IServiceScopeFactory>();

        // Act
        await using var actScope = serviceScopeProvider.CreateAsyncScope();
        var actDbContext = actScope.ServiceProvider.GetRequiredService<AppReadDbContext>();

        var paginationQuery = new PaginationQuery
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await actDbContext.Vehicles
            .AsNoTracking()
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        // Assert
        result.Items.Count().ShouldBe(10);
        result.TotalCount.ShouldBe(100);
        result.PageNumber.ShouldBe(1);
    }

    [Fact]
    public async Task PaginationShouldCorrectlyManagePageSize()
    {
        var serviceScopeProvider = factory.Services.GetRequiredService<IServiceScopeFactory>();
        await using var actScope = serviceScopeProvider.CreateAsyncScope();
        var actDbContext = actScope.ServiceProvider.GetRequiredService<AppReadDbContext>();

        var paginationQuery = new PaginationQuery
        {
            PageNumber = 1,
            PageSize = 11
        };

        var result = await actDbContext.Vehicles
            .AsNoTracking()
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        // Assert
        result.Items.Count().ShouldBe(11);
        result.TotalCount.ShouldBe(100);
        result.PageNumber.ShouldBe(1);
    }

    [Fact]
    public async Task PaginationShouldReturnEmptyResultForInvalidPageNumber()
    {
        var serviceScopeProvider = factory.Services.GetRequiredService<IServiceScopeFactory>();
        await using var actScope = serviceScopeProvider.CreateAsyncScope();
        var actDbContext = actScope.ServiceProvider.GetRequiredService<AppReadDbContext>();

        var paginationQuery = new PaginationQuery
        {
            PageNumber = 1000,
            PageSize = 10
        };

        var result = await actDbContext.Vehicles
            .AsNoTracking()
            .PaginateAsync(paginationQuery.PageNumber, paginationQuery.PageSize);

        // Assert
        result.Items.Count().ShouldBe(0);
        result.TotalCount.ShouldBe(100);
        result.PageNumber.ShouldBe(1000);
    }
}