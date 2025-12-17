using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFinancialSettings_RemovePackageAndLiveEvent_AddProductShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerLiveEventSharePercentage",
                table: "FinancialSettings");

            migrationBuilder.RenameColumn(
                name: "SellerPackageSharePercentage",
                table: "FinancialSettings",
                newName: "SellerProductSharePercentage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellerProductSharePercentage",
                table: "FinancialSettings",
                newName: "SellerPackageSharePercentage");

            migrationBuilder.AddColumn<decimal>(
                name: "SellerLiveEventSharePercentage",
                table: "FinancialSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
