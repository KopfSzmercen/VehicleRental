using System.Runtime.CompilerServices;
using VehicleRental.Persistence;
using VehicleRental.Users;
using VehicleRental.Vehicles;

[assembly: InternalsVisibleTo("VehicleRental.Tests.Integration")]
[assembly: InternalsVisibleTo("VehicleRental.Tests.Unit")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddVehiclesModule(builder.Configuration); // Add this line

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "VehicleRental"));
}

app.UseHttpsRedirection();

app.UseUsersModule();
app.UseVehiclesModule(); // Add this line

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