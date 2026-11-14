using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreScrapeSuspension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoSuspendedUntilUtc",
                table: "Stores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveScrapeFailures",
                table: "Stores",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoSuspendedUntilUtc",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ConsecutiveScrapeFailures",
                table: "Stores");
        }
    }
}
