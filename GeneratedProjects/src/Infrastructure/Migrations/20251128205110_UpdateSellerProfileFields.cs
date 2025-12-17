using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSellerProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Degree",
                table: "SellerProfiles");

            migrationBuilder.RenameColumn(
                name: "Specialty",
                table: "SellerProfiles",
                newName: "WorkingHours");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceYears",
                table: "SellerProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenseExpiryDate",
                table: "SellerProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LicenseIssueDate",
                table: "SellerProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseNumber",
                table: "SellerProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopAddress",
                table: "SellerProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExperienceYears",
                table: "SellerProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseExpiryDate",
                table: "SellerProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseIssueDate",
                table: "SellerProfiles");

            migrationBuilder.DropColumn(
                name: "LicenseNumber",
                table: "SellerProfiles");

            migrationBuilder.DropColumn(
                name: "ShopAddress",
                table: "SellerProfiles");

            migrationBuilder.RenameColumn(
                name: "WorkingHours",
                table: "SellerProfiles",
                newName: "Specialty");

            migrationBuilder.AddColumn<string>(
                name: "Degree",
                table: "SellerProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
