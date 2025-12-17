using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeoTemplate01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SeoMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturedImageAlt",
                table: "SeoMetadata",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturedImageUrl",
                table: "SeoMetadata",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "H1Title",
                table: "SeoMetadata",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "SeoMetadata",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "FeaturedImageAlt",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "FeaturedImageUrl",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "H1Title",
                table: "SeoMetadata");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "SeoMetadata");
        }
    }
}
