using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperTypeToStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScraperType",
                table: "Stores",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScraperType",
                table: "Stores");
        }
    }
}
