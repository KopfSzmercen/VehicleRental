using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleRental.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatusToRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Rentals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Rentals",
                type: "text",
                nullable: true);

            // Rentals completed before payments existed count as paid, so historical data blocks nobody.
            // Active and Cancelled rentals keep a null payment status.
            migrationBuilder.Sql(
                """
                UPDATE "Rentals"
                SET "PaymentStatus" = 'Paid',
                    "PaidAt" = "CompletedAt"
                WHERE "Status" = 'Completed';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Rentals");
        }
    }
}
