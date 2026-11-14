using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackedProductDiscoveryTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketCode",
                table: "ProductSearchSessions",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceProductId",
                table: "ProductSearchSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSearchSessions_SourceProductId_MarketCode_CreatedAtU~",
                table: "ProductSearchSessions",
                columns: new[] { "SourceProductId", "MarketCode", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductSearchSessions_SourceProductId_MarketCode_CreatedAtU~",
                table: "ProductSearchSessions");

            migrationBuilder.DropColumn(
                name: "MarketCode",
                table: "ProductSearchSessions");

            migrationBuilder.DropColumn(
                name: "SourceProductId",
                table: "ProductSearchSessions");
        }
    }
}
