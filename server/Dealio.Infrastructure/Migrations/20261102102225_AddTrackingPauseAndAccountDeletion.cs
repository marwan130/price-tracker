using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dealio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingPauseAndAccountDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionRequestedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionScheduledAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackingPausedBeforeDeletion",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "idx_users_deletion_scheduled_at_utc",
                table: "Users",
                column: "DeletionScheduledAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_users_deletion_scheduled_at_utc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletionRequestedAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletionScheduledAtUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TrackingPausedBeforeDeletion",
                table: "Users");
        }
    }
}
