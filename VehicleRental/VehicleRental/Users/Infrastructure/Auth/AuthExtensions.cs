using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VehicleRental.Persistence;
using VehicleRental.Users.Domain;
using VehicleRental.Users.Infrastructure.Tokens;

namespace VehicleRental.Users.Infrastructure.Auth;

internal static class AuthExtensions
{
    public static void AddAuth(this IServiceCollection services)
    {
        services.AddSingleton<ITokensManager, TokensManager>();

        services.AddAuthorization();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 5;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredUniqueChars = 0;
        });

        var tokenOptions = services.BuildServiceProvider()
            .GetRequiredService<IOptions<TokensOptions>>()
            .Value;

        services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = AuthorizationSchemes.SmartScheme;
                o.DefaultChallengeScheme = AuthorizationSchemes.SmartScheme;
            })
            .AddPolicyScheme(AuthorizationSchemes.SmartScheme, "Bearer or Cookies", o =>
            {
                o.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization;

                    var isJwtBearerVariant = !string.IsNullOrEmpty(authHeader) &&
                                             authHeader.ToString().StartsWith(JwtBearerDefaults.AuthenticationScheme);

                    return isJwtBearerVariant
                        ? JwtBearerDefaults.AuthenticationScheme
                        : IdentityConstants.ApplicationScheme;
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenOptions.Secret)),
                    RequireAudience = false,
                    ValidateAudience = false,
                    ValidateIssuer = false
                };
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = tokenOptions.Issuer + "-auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToLogout = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });

        services
            .AddIdentityCore<User>()
            .AddRoles<UserRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<SignInManager<User>>()
            .AddDefaultTokenProviders();

        services.AddHostedService<UserRolesInitializer>();

        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>();
    }
}