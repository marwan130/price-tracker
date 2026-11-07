using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchCandidateVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletionPayload",
                table: "ProductSearchSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingVerifications",
                table: "ProductSearchSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedCandidates",
                table: "ProductSearchSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionPayload",
                table: "ProductSearchSessions");

            migrationBuilder.DropColumn(
                name: "PendingVerifications",
                table: "ProductSearchSessions");

            migrationBuilder.DropColumn(
                name: "VerifiedCandidates",
                table: "ProductSearchSessions");
        }
    }
}
