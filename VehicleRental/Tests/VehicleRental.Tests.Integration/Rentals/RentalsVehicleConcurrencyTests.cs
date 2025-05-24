using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VehicleRental.Common.ErrorHandling;
using VehicleRental.Persistence;
using VehicleRental.Rentals.Domain;

namespace VehicleRental.Tests.Integration.Rentals;

[Collection(nameof(IntegrationTestsCollectionFixture))]
public class RentalsVehicleConcurrencyTests(TestWebApplication testWebApplication) : IDisposable
{
    public void Dispose()
    {
        testWebApplication.ResetDatabaseAsync().Wait();
    }


    [Fact]
    public async Task ConcurrencyColumn_OnRentalsVehicleAggregate_ShouldHandleOptimisicLocking()
    {
        var rentalVehicleId = Guid.NewGuid();

        // Arrange
        await using (var scope = testWebApplication.Services.CreateAsyncScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();


            var rentalVehicle = RentalsVehicle.CreateNew(rentalVehicleId, DateTimeOffset.Now);

            dbContext.RentalVehicles.Add(rentalVehicle);
            await dbContext.SaveChangesAsync();
        }

        // Act
        await using (var scope = testWebApplication.Services.CreateAsyncScope())
        {
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<AppDbContext>();

            var rental = Rental.CreateNew(
                rentalVehicleId,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2),
                new Money(600, Currency.EUR),
                DateTimeOffset.Now
            );

            var rentalVehicle = await dbContext.RentalVehicles.FindAsync(rentalVehicleId);

            await using (var scope2 = testWebApplication.Services.CreateAsyncScope())
            {
                var scopedServices2 = scope2.ServiceProvider;
                var dbContext2 = scopedServices2.GetRequiredService<AppDbContext>();

                await dbContext2.RentalVehicles.Where(rv => rv.Id == rentalVehicleId)
                    .ExecuteUpdateAsync(u => u.SetProperty(r => r.UpdatedAt, DateTimeOffset.Now.AddMinutes(5)));
            }


            rentalVehicle!.Rent(rental, DateTimeOffset.Now.AddMinutes(10));

            var unitOfWork = scopedServices.GetRequiredService<IUnitOfWork>();

            var exception =
                await Should.ThrowAsync<BusinessRuleValidationException>(async () =>
                    await unitOfWork.SaveChangesAsync());

            exception.Message.ShouldBe(
                "The data you are trying to update has been modified by another user. Please reload the data and try again.");
        }
    }
}