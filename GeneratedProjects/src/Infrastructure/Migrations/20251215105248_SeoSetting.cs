using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeoSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PageFaqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageType = table.Column<int>(type: "int", nullable: false),
                    PageIdentifier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_PageFaqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageFaqs_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PageFaqs_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeoMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageType = table.Column<int>(type: "int", nullable: false),
                    PageIdentifier = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    MetaRobots = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OgTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OgDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OgImage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OgType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OgUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TwitterCard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwitterTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TwitterDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TwitterImage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BreadcrumbsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SitemapPriority = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    SitemapChangefreq = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_SeoMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeoMetadata_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeoMetadata_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageFaqs_CreatorId",
                table: "PageFaqs",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PageFaqs_PageType_PageIdentifier_DisplayOrder",
                table: "PageFaqs",
                columns: new[] { "PageType", "PageIdentifier", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PageFaqs_UpdaterId",
                table: "PageFaqs",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoMetadata_CreatorId",
                table: "SeoMetadata",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoMetadata_PageType",
                table: "SeoMetadata",
                column: "PageType",
                filter: "[PageIdentifier] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SeoMetadata_PageType_PageIdentifier",
                table: "SeoMetadata",
                columns: new[] { "PageType", "PageIdentifier" },
                unique: true,
                filter: "[PageIdentifier] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SeoMetadata_UpdaterId",
                table: "SeoMetadata",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageFaqs");

            migrationBuilder.DropTable(
                name: "SeoMetadata");
        }
    }
}
