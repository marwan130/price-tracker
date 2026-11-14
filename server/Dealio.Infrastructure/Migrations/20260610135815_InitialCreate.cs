using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PriceTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

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
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
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
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Brand = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stores", x => x.StoreId);
                    table.ForeignKey(
                        name: "FK_Stores_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ImageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
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
                name: "StoreProductListings",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastScrapedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProductListings", x => x.ListingId);
                    table.ForeignKey(
                        name: "FK_StoreProductListings_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreProductListings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoreProductListings_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
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

            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PriceInUsd = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScrapedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                    table.UniqueConstraint("AK_PriceHistories_ListingId_RecordedAt", x => new { x.ListingId, x.RecordedAt });
                    table.ForeignKey(
                        name: "FK_PriceHistories_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceHistories_StoreProductListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "StoreProductListings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeLogs",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ItemsScraped = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_ScrapeLogs_StoreProductListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "StoreProductListings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScrapeLogs_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "StoreId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProductTrackings",
                columns: table => new
                {
                    TrackingId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyEmail = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductTrackings", x => x.TrackingId);
                    table.ForeignKey(
                        name: "FK_UserProductTrackings_Currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProductTrackings_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductTrackings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductTrackings_StoreProductListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "StoreProductListings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserProductTrackings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrackingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    TriggeredPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Channel = table.Column<string>(type: "text", nullable: false, defaultValue: "Email"),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_PriceHistories_PriceHistoryId",
                        column: x => x.PriceHistoryId,
                        principalTable: "PriceHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_UserProductTrackings_TrackingId",
                        column: x => x.TrackingId,
                        principalTable: "UserProductTrackings",
                        principalColumn: "TrackingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "idx_categories_name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_sent_at",
                table: "Notifications",
                column: "SentAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_status",
                table: "Notifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_tracking",
                table: "Notifications",
                column: "TrackingId");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PriceHistoryId",
                table: "Notifications",
                column: "PriceHistoryId");

            migrationBuilder.CreateIndex(
                name: "idx_price_history_currency",
                table: "PriceHistories",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "idx_price_history_listing_scrape",
                table: "PriceHistories",
                columns: new[] { "ListingId", "ScrapedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_price_history_listing_time",
                table: "PriceHistories",
                columns: new[] { "ListingId", "RecordedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_price_history_recorded_at",
                table: "PriceHistories",
                column: "RecordedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_product_images_product",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_products_brand",
                table: "Products",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "idx_products_category",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "idx_products_name_fts",
                table: "Products",
                column: "Name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

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
                name: "idx_scrape_logs_listing",
                table: "ScrapeLogs",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "idx_scrape_logs_started",
                table: "ScrapeLogs",
                column: "StartedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_scrape_logs_status",
                table: "ScrapeLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_scrape_logs_store",
                table: "ScrapeLogs",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "idx_listings_active",
                table: "StoreProductListings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "idx_listings_product",
                table: "StoreProductListings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_listings_scraped_at",
                table: "StoreProductListings",
                column: "LastScrapedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_listings_store",
                table: "StoreProductListings",
                column: "StoreId");

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
                name: "idx_stores_country",
                table: "Stores",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "idx_stores_currency_code",
                table: "Stores",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_active",
                table: "UserProductTrackings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_listing",
                table: "UserProductTrackings",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_product",
                table: "UserProductTrackings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_unique",
                table: "UserProductTrackings",
                columns: new[] { "UserId", "ProductId", "VariantId", "ListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_tracking_user",
                table: "UserProductTrackings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_tracking_variant",
                table: "UserProductTrackings",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductTrackings_CurrencyCode",
                table: "UserProductTrackings",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_variant_attributes_value",
                table: "VariantAttributes",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "idx_variant_attributes_variant",
                table: "VariantAttributes",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ScrapeLogs");

            migrationBuilder.DropTable(
                name: "VariantAttributes");

            migrationBuilder.DropTable(
                name: "PriceHistories");

            migrationBuilder.DropTable(
                name: "UserProductTrackings");

            migrationBuilder.DropTable(
                name: "AttributeValues");

            migrationBuilder.DropTable(
                name: "StoreProductListings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AttributeTypes");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
