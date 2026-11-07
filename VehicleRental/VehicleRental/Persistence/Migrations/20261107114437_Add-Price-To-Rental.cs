using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleRental.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceToRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceAmount",
                table: "Rentals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PriceCurrency",
                table: "Rentals",
                type: "text",
                nullable: false,
                defaultValue: "");

            // 10 USD per started hour of [StartDate, EndDate), the same rule as Rental.CreateNew.
            migrationBuilder.Sql(
                """
                UPDATE "Rentals"
                SET "PriceAmount" = 10 * CEIL(EXTRACT(EPOCH FROM ("EndDate" - "StartDate")) / 3600)::integer,
                    "PriceCurrency" = 'USD';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceAmount",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                table: "Rentals");
        }
    }
}
