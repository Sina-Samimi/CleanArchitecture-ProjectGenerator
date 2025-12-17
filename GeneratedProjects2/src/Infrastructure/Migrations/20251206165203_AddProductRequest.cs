using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrackInventory = table.Column<bool>(type: "bit", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeaturedImagePath = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DigitalDownloadPath = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    SellerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SeoTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SeoDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SeoKeywords = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SeoSlug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Robots = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCustomOrder = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApprovedProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRequests_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRequests_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRequests_Products_ApprovedProductId",
                        column: x => x.ApprovedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductRequests_SiteCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SiteCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductRequestImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRequestImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRequestImages_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRequestImages_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductRequestImages_ProductRequests_ProductRequestId",
                        column: x => x.ProductRequestId,
                        principalTable: "ProductRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequestImages_CreatorId",
                table: "ProductRequestImages",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequestImages_ProductRequestId",
                table: "ProductRequestImages",
                column: "ProductRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequestImages_UpdaterId",
                table: "ProductRequestImages",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_ApprovedProductId",
                table: "ProductRequests",
                column: "ApprovedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_CategoryId",
                table: "ProductRequests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_CreateDate",
                table: "ProductRequests",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_CreatorId",
                table: "ProductRequests",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_SellerId",
                table: "ProductRequests",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_SeoSlug",
                table: "ProductRequests",
                column: "SeoSlug");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_Status",
                table: "ProductRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_UpdaterId",
                table: "ProductRequests",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductRequestImages");

            migrationBuilder.DropTable(
                name: "ProductRequests");
        }
    }
}
