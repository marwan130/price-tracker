using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dealio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductVariantsAndAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreProductListings_ProductVariants_VariantId",
                table: "StoreProductListings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProductTrackings_ProductVariants_VariantId",
                table: "UserProductTrackings");

            migrationBuilder.DropTable(
                name: "VariantAttributes");

            migrationBuilder.DropTable(
                name: "AttributeValues");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "AttributeTypes");

            migrationBuilder.DropIndex(
                name: "idx_tracking_unique",
                table: "UserProductTrackings");

            migrationBuilder.DropIndex(
                name: "idx_tracking_variant",
                table: "UserProductTrackings");

            migrationBuilder.DropIndex(
                name: "idx_listings_variant",
                table: "StoreProductListings");

            migrationBuilder.DropIndex(
                name: "idx_listings_variant_store",
                table: "StoreProductListings");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "UserProductTrackings");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "StoreProductListings");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_unique",
                table: "UserProductTrackings",
                columns: new[] { "UserId", "ProductId", "ListingId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tracking_unique",
                table: "UserProductTrackings");

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "UserProductTrackings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VariantId",
                table: "StoreProductListings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "AttributeTypes",
                columns: table => new
                {
                    AttributeTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeTypes", x => x.AttributeTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Sku = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.VariantId);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttributeValues",
                columns: table => new
                {
                    AttributeValueId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttributeTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeValues", x => x.AttributeValueId);
                    table.ForeignKey(
                        name: "FK_AttributeValues_AttributeTypes_AttributeTypeId",
                        column: x => x.AttributeTypeId,
                        principalTable: "AttributeTypes",
                        principalColumn: "AttributeTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariantAttributes",
                columns: table => new
                {
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeValueId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantAttributes", x => new { x.VariantId, x.AttributeValueId });
                    table.ForeignKey(
                        name: "FK_VariantAttributes_AttributeValues_AttributeValueId",
                        column: x => x.AttributeValueId,
                        principalTable: "AttributeValues",
                        principalColumn: "AttributeValueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VariantAttributes_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_tracking_unique",
                table: "UserProductTrackings",
                columns: new[] { "UserId", "ProductId", "VariantId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tracking_variant",
                table: "UserProductTrackings",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "idx_listings_variant",
                table: "StoreProductListings",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "idx_listings_variant_store",
                table: "StoreProductListings",
                columns: new[] { "VariantId", "StoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_attribute_types_name",
                table: "AttributeTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_attribute_values_type",
                table: "AttributeValues",
                column: "AttributeTypeId");

            migrationBuilder.CreateIndex(
                name: "idx_attribute_values_type_value",
                table: "AttributeValues",
                columns: new[] { "AttributeTypeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_product_variants_active",
                table: "ProductVariants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "idx_product_variants_product",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_product_variants_product_sku",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Sku" },
                unique: true,
                filter: "\"Sku\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_product_variants_sku",
                table: "ProductVariants",
                column: "Sku");

            migrationBuilder.CreateIndex(
                name: "idx_variant_attributes_value",
                table: "VariantAttributes",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "idx_variant_attributes_variant",
                table: "VariantAttributes",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreProductListings_ProductVariants_VariantId",
                table: "StoreProductListings",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "VariantId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProductTrackings_ProductVariants_VariantId",
                table: "UserProductTrackings",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "VariantId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
