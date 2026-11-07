using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleRental.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsActiveWithReservationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Reservations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Reservations"
                SET "Status" = CASE
                    WHEN "IsActive" THEN 'Active'
                    WHEN "CancelledAt" IS NOT NULL THEN 'Cancelled'
                    ELSE 'Expired'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "Reservations"
                SET "IsActive" = ("Status" = 'Active');
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reservations");
        }
    }
}
