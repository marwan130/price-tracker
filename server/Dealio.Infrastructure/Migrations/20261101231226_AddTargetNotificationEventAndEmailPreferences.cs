using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetNotificationEventAndEmailPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyFurtherDecreaseEmail",
                table: "UserProductTrackings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "TargetReached");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyFurtherDecreaseEmail",
                table: "UserProductTrackings");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Notifications");
        }
    }
}
