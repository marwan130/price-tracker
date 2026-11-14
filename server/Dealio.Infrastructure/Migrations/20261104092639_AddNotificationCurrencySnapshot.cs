using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCurrencySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConversionRate",
                table: "Notifications",
                type: "numeric(20,10)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConvertedTriggeredPrice",
                table: "Notifications",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RateUpdatedAtUtc",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversionRate",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ConvertedTriggeredPrice",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RateUpdatedAtUtc",
                table: "Notifications");
        }
    }
}
