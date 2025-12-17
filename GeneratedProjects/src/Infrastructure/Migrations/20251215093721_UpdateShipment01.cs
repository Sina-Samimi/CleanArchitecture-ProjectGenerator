using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShipment01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCartItems_CartId_ProductId_VariantId",
                table: "ShoppingCartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "OfferId",
                table: "ShoppingCartItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCartItems_CartId_ProductId_VariantId_OfferId",
                table: "ShoppingCartItems",
                columns: new[] { "CartId", "ProductId", "VariantId", "OfferId" },
                unique: true,
                filter: "[VariantId] IS NOT NULL AND [OfferId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCartItems_CartId_ProductId_VariantId_OfferId",
                table: "ShoppingCartItems");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "ShoppingCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCartItems_CartId_ProductId_VariantId",
                table: "ShoppingCartItems",
                columns: new[] { "CartId", "ProductId", "VariantId" },
                unique: true,
                filter: "[VariantId] IS NOT NULL");
        }
    }
}
