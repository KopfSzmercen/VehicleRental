using System.Runtime.CompilerServices;
using VehicleRental.Persistence;
using VehicleRental.Users;

[assembly: InternalsVisibleTo("VehicleRental.Tests.Integration")]
[assembly: InternalsVisibleTo("VehicleRental.Tests.Unit")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddUsersModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "VehicleRental"));
}

app.UseHttpsRedirection();

app.UseUsersModule();

await app.ApplyMigrations();

app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();

namespace VehicleRental
{
    public class Program
    {
    }
}