using System.Runtime.CompilerServices;
using VehicleRental.Common;
using VehicleRental.Persistence;
using VehicleRental.Rentals;
using VehicleRental.Users;
using VehicleRental.Vehicles;

[assembly: InternalsVisibleTo("VehicleRental.Tests.Integration")]
[assembly: InternalsVisibleTo("VehicleRental.Tests.Unit")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); });

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddVehiclesModule(builder.Configuration);
builder.Services.AddRentalsModule(builder.Configuration);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "VehicleRental"));
}

app.UseHttpsRedirection();

app.UseUsersModule();
app.UseVehiclesModule();
app.UseRentalsModule();

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