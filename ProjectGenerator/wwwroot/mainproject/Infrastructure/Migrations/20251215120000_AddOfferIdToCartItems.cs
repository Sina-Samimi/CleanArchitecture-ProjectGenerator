using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferIdToCartItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old indexes if they exist (handles re-run or partial runs)
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ShoppingCartItems_CartId_ProductId_VariantId_OfferId' AND object_id = OBJECT_ID(N'[dbo].[ShoppingCartItems]'))
                BEGIN
                    DROP INDEX [IX_ShoppingCartItems_CartId_ProductId_VariantId_OfferId] ON [dbo].[ShoppingCartItems];
                END
            """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ShoppingCartItems_CartId_ProductId_VariantId' AND object_id = OBJECT_ID(N'[dbo].[ShoppingCartItems]'))
                BEGIN
                    DROP INDEX [IX_ShoppingCartItems_CartId_ProductId_VariantId] ON [dbo].[ShoppingCartItems];
                END
            """);

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
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ShoppingCartItems_CartId_ProductId_VariantId_OfferId' AND object_id = OBJECT_ID(N'[dbo].[ShoppingCartItems]'))
                BEGIN
                    DROP INDEX [IX_ShoppingCartItems_CartId_ProductId_VariantId_OfferId] ON [dbo].[ShoppingCartItems];
                END
            """);

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

