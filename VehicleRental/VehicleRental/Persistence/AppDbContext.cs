using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Users.Domain;

namespace VehicleRental.Persistence;

internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) :
    IdentityDbContext<
        User,
        UserRole,
        Guid
    >(options), IDataProtectionKeyContext


{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}