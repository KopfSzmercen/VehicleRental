using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VehicleRental.Users.Domain;
using VehicleRental.Users.Infrastructure.Tokens;

namespace VehicleRental.Tests.Integration;

internal static class HttpClientExtensions
{
    public static async Task<HttpClient> SignInAsAdminAsync(this HttpClient client,
        TestWebApplication testWebApplication)
    {
        using var scope = testWebApplication.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = User.CreateNormalUser("admin@admin.com");

        await userManager.CreateAsync(user, "adminSecurePassword123");

        await userManager.AddToRoleAsync(user, UserRole.Admin);

        var createdUser = await userManager.FindByEmailAsync("admin@admin.com");

        await userManager.AddClaimsAsync(user, [
            new Claim("UserId", createdUser!.Id.ToString()),
            new Claim("SendEmails", true.ToString())
        ]);

        var tokensManager = scope.ServiceProvider.GetRequiredService<ITokensManager>();

        var token = tokensManager.CreateToken(createdUser.Id, [UserRole.Admin], []);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        return client;
    }
}