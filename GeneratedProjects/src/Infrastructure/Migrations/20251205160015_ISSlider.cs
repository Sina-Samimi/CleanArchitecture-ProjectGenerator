using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ISSlider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Banners_IsSlider",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "IsSlider",
                table: "Banners");

            migrationBuilder.AddColumn<bool>(
                name: "BannersAsSlider",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannersAsSlider",
                table: "SiteSettings");

            migrationBuilder.AddColumn<bool>(
                name: "IsSlider",
                table: "Banners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Banners_IsSlider",
                table: "Banners",
                column: "IsSlider");
        }
    }
}
