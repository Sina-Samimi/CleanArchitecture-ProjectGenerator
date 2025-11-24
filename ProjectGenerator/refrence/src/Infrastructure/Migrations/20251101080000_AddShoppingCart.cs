using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ShoppingCarts table only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShoppingCarts')
                BEGIN
                    CREATE TABLE [ShoppingCarts] (
                        [Id] uniqueidentifier NOT NULL,
                        [AnonymousId] uniqueidentifier NULL,
                        [UserId] nvarchar(450) NULL,
                        [AppliedDiscountCode] nvarchar(100) NULL,
                        [AppliedDiscountType] int NULL,
                        [AppliedDiscountValue] decimal(18,2) NULL,
                        [AppliedDiscountAmount] decimal(18,2) NULL,
                        [AppliedDiscountWasCapped] bit NOT NULL DEFAULT 0,
                        [DiscountEvaluatedAt] datetimeoffset NULL,
                        [DiscountOriginalSubtotal] decimal(18,2) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        CONSTRAINT [PK_ShoppingCarts] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ShoppingCarts_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_ShoppingCarts_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_ShoppingCarts_AnonymousId] ON [ShoppingCarts] ([AnonymousId]) WHERE [AnonymousId] IS NOT NULL;
                    CREATE INDEX [IX_ShoppingCarts_CreatorId] ON [ShoppingCarts] ([CreatorId]);
                    CREATE INDEX [IX_ShoppingCarts_UpdaterId] ON [ShoppingCarts] ([UpdaterId]);
                    CREATE INDEX [IX_ShoppingCarts_UserId] ON [ShoppingCarts] ([UserId]) WHERE [UserId] IS NOT NULL;
                END
            ");

            // Create ShoppingCartItems table only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShoppingCartItems')
                BEGIN
                    CREATE TABLE [ShoppingCartItems] (
                        [Id] uniqueidentifier NOT NULL,
                        [ProductId] uniqueidentifier NOT NULL,
                        [ProductName] nvarchar(300) NOT NULL,
                        [ProductSlug] nvarchar(250) NOT NULL,
                        [ThumbnailPath] nvarchar(600) NULL,
                        [UnitPrice] decimal(18,2) NOT NULL,
                        [CompareAtPrice] decimal(18,2) NULL,
                        [Quantity] int NOT NULL DEFAULT 1,
                        [ProductType] int NOT NULL,
                        [CartId] uniqueidentifier NOT NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        CONSTRAINT [PK_ShoppingCartItems] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ShoppingCartItems_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_ShoppingCartItems_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_ShoppingCartItems_ShoppingCarts_CartId] FOREIGN KEY ([CartId]) REFERENCES [ShoppingCarts] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_ShoppingCartItems_CartId] ON [ShoppingCartItems] ([CartId]);
                    CREATE UNIQUE INDEX [IX_ShoppingCartItems_CartId_ProductId] ON [ShoppingCartItems] ([CartId], [ProductId]);
                    CREATE INDEX [IX_ShoppingCartItems_CreatorId] ON [ShoppingCartItems] ([CreatorId]);
                    CREATE INDEX [IX_ShoppingCartItems_UpdaterId] ON [ShoppingCartItems] ([UpdaterId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingCartItems");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");
        }
    }
}
