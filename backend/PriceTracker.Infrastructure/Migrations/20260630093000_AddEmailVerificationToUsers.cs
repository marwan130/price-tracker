using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationTokenHash",
                table: "Users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_email_verification_token_hash",
                table: "Users",
                column: "EmailVerificationTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_users_email_verification_token_hash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "Users");
        }
    }
}
