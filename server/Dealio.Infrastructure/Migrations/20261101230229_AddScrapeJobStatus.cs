using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapeJobStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "OutboxMessages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByUserId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "OutboxMessages");
        }
    }
}
