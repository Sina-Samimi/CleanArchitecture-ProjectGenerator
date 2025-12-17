using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class smssetting01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerLinkAlertTemplate",
                table: "SmsSettings");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmTemplate",
                table: "SmsSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerLinkAlertTemplate",
                table: "SmsSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumberConfirmTemplate",
                table: "SmsSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
