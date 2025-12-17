using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOffersAndTargetProductId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetProductId",
                table: "ProductRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductOffers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CompareAtPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrackInventory = table.Column<bool>(type: "bit", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedFromRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ProductOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOffers_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductOffers_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductOffers_ProductRequests_ApprovedFromRequestId",
                        column: x => x.ApprovedFromRequestId,
                        principalTable: "ProductRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductOffers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_TargetProductId",
                table: "ProductRequests",
                column: "TargetProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_ApprovedFromRequestId",
                table: "ProductOffers",
                column: "ApprovedFromRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_CreateDate",
                table: "ProductOffers",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_CreatorId",
                table: "ProductOffers",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_IsActive",
                table: "ProductOffers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_IsPublished",
                table: "ProductOffers",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_ProductId",
                table: "ProductOffers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_ProductId_SellerId",
                table: "ProductOffers",
                columns: new[] { "ProductId", "SellerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_SellerId",
                table: "ProductOffers",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOffers_UpdaterId",
                table: "ProductOffers",
                column: "UpdaterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductRequests_Products_TargetProductId",
                table: "ProductRequests",
                column: "TargetProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductRequests_Products_TargetProductId",
                table: "ProductRequests");

            migrationBuilder.DropTable(
                name: "ProductOffers");

            migrationBuilder.DropIndex(
                name: "IX_ProductRequests_TargetProductId",
                table: "ProductRequests");

            migrationBuilder.DropColumn(
                name: "TargetProductId",
                table: "ProductRequests");
        }
    }
}
