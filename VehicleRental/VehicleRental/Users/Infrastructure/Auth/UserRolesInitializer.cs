using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleRental.Users.Domain;

namespace VehicleRental.Users.Infrastructure.Auth;

public sealed class UserRolesInitializer(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();

        foreach (var role in UserRole.AvailableRoles)
            if (await roleManager.FindByNameAsync(role) is null)
                await roleManager.CreateAsync(new UserRole(role));


        var allRoles
            = await roleManager.Roles.ToListAsync(stoppingToken);

        foreach (var role in allRoles.Where(role => !UserRole.AvailableRoles.Contains(role.Name)))
            await roleManager.DeleteAsync(role);
    }
}