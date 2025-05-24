using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleRental.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyColumnRentalsVehicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RentalVehicles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RentalVehicles");
        }
    }
}
