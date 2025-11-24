using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCommentApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorRole",
                table: "ProductComments");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "ProductComments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "ProductComments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "BlogComments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "BlogComments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductComments_ApprovedById",
                table: "ProductComments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_ApprovedById",
                table: "BlogComments",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_BlogComments_AspNetUsers_ApprovedById",
                table: "BlogComments",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductComments_AspNetUsers_ApprovedById",
                table: "ProductComments",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlogComments_AspNetUsers_ApprovedById",
                table: "BlogComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductComments_AspNetUsers_ApprovedById",
                table: "ProductComments");

            migrationBuilder.DropIndex(
                name: "IX_BlogComments_ApprovedById",
                table: "BlogComments");

            migrationBuilder.DropIndex(
                name: "IX_ProductComments_ApprovedById",
                table: "ProductComments");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "BlogComments");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "BlogComments");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "ProductComments");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ProductComments");

            migrationBuilder.AddColumn<string>(
                name: "AuthorRole",
                table: "ProductComments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
