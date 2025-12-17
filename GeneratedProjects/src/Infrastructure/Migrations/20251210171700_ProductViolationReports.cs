using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductViolationReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductViolationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductOfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SellerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReporterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReporterPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_ProductViolationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductViolationReports_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductViolationReports_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductViolationReports_ProductOffers_ProductOfferId",
                        column: x => x.ProductOfferId,
                        principalTable: "ProductOffers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductViolationReports_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_CreatorId",
                table: "ProductViolationReports",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_ProductId",
                table: "ProductViolationReports",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_ProductId_CreateDate",
                table: "ProductViolationReports",
                columns: new[] { "ProductId", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_ProductOfferId",
                table: "ProductViolationReports",
                column: "ProductOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_ReporterId",
                table: "ProductViolationReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_SellerId",
                table: "ProductViolationReports",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_SellerId_IsReviewed",
                table: "ProductViolationReports",
                columns: new[] { "SellerId", "IsReviewed" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductViolationReports_UpdaterId",
                table: "ProductViolationReports",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductViolationReports");
        }
    }
}
