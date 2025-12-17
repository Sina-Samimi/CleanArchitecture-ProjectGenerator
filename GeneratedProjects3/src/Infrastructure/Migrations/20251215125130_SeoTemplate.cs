using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeoTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionTemplate",
                table: "SeoMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgDescriptionTemplate",
                table: "SeoMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgTitleTemplate",
                table: "SeoMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RobotsTemplate",
                table: "SeoMetadata",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleTemplate",
                table: "SeoMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseTemplate",
                table: "SeoMetadata",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SeoOgImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeoMetadataId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    ImageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Alt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_SeoOgImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeoOgImages_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeoOgImages_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeoOgImages_SeoMetadata_SeoMetadataId",
                        column: x => x.SeoMetadataId,
                        principalTable: "SeoMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeoOgImages_CreatorId",
                table: "SeoOgImages",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoOgImages_SeoMetadataId_DisplayOrder",
                table: "SeoOgImages",
                columns: new[] { "SeoMetadataId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoOgImages_UpdaterId",
                table: "SeoOgImages",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeoOgImages");

            migrationBuilder.DropColumn(
                name: "DescriptionTemplate",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "OgDescriptionTemplate",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "OgTitleTemplate",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "RobotsTemplate",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "TitleTemplate",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "UseTemplate",
                table: "SeoMetadata");
        }
    }
}
