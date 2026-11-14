using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    public partial class AddScheduledOfferRefresh : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefreshQueuedAtUtc",
                table: "StoreProductListings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_listings_refresh_queued_at",
                table: "StoreProductListings",
                column: "LastRefreshQueuedAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_listings_refresh_queued_at",
                table: "StoreProductListings");

            migrationBuilder.DropColumn(
                name: "LastRefreshQueuedAtUtc",
                table: "StoreProductListings");
        }
    }
}
