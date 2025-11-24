using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAllTablesExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration ensures all tables exist by checking before creating
            // This prevents "object already exists" errors
            
            migrationBuilder.Sql(@"
                -- Ensure WalletAccounts table exists
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WalletAccounts')
                BEGIN
                    CREATE TABLE [WalletAccounts] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        [Currency] nvarchar(16) NOT NULL,
                        [Balance] decimal(18,2) NOT NULL DEFAULT 0,
                        [IsLocked] bit NOT NULL,
                        [LastActivityOn] datetimeoffset NOT NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        CONSTRAINT [PK_WalletAccounts] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_WalletAccounts_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_WalletAccounts_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_WalletAccounts_CreatorId] ON [WalletAccounts] ([CreatorId]);
                    CREATE INDEX [IX_WalletAccounts_UpdaterId] ON [WalletAccounts] ([UpdaterId]);
                    CREATE UNIQUE INDEX [IX_WalletAccounts_UserId] ON [WalletAccounts] ([UserId]);
                END

                -- Ensure WalletTransactions table exists
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WalletTransactions')
                BEGIN
                    CREATE TABLE [WalletTransactions] (
                        [Id] uniqueidentifier NOT NULL,
                        [WalletAccountId] uniqueidentifier NOT NULL,
                        [Amount] decimal(18,2) NOT NULL,
                        [Type] int NOT NULL,
                        [Status] int NOT NULL,
                        [BalanceAfterTransaction] decimal(18,2) NOT NULL,
                        [Reference] nvarchar(64) NOT NULL,
                        [Description] nvarchar(512) NULL,
                        [Metadata] nvarchar(2000) NULL,
                        [InvoiceId] uniqueidentifier NULL,
                        [PaymentTransactionId] uniqueidentifier NULL,
                        [OccurredAt] datetimeoffset NOT NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_WalletTransactions_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_WalletTransactions_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_WalletTransactions_WalletAccounts_WalletAccountId] FOREIGN KEY ([WalletAccountId]) REFERENCES [WalletAccounts] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_WalletTransactions_CreatorId] ON [WalletTransactions] ([CreatorId]);
                    CREATE INDEX [IX_WalletTransactions_InvoiceId] ON [WalletTransactions] ([InvoiceId]);
                    CREATE INDEX [IX_WalletTransactions_PaymentTransactionId] ON [WalletTransactions] ([PaymentTransactionId]);
                    CREATE INDEX [IX_WalletTransactions_UpdaterId] ON [WalletTransactions] ([UpdaterId]);
                    CREATE INDEX [IX_WalletTransactions_WalletAccountId] ON [WalletTransactions] ([WalletAccountId]);
                END

                -- Ensure ShoppingCarts table exists
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

                -- Ensure ShoppingCartItems table exists
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

                -- Ensure PageAccessPolicies table exists
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PageAccessPolicies')
                BEGIN
                    CREATE TABLE [PageAccessPolicies] (
                        [Id] uniqueidentifier NOT NULL,
                        [Area] nvarchar(64) NOT NULL DEFAULT '',
                        [Controller] nvarchar(128) NOT NULL,
                        [Action] nvarchar(128) NOT NULL,
                        [PermissionKey] nvarchar(128) NOT NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        CONSTRAINT [PK_PageAccessPolicies] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PageAccessPolicies_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_PageAccessPolicies_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_PageAccessPolicies_Area_Controller_Action] ON [PageAccessPolicies] ([Area], [Controller], [Action]);
                    CREATE UNIQUE INDEX [IX_PageAccessPolicies_Area_Controller_Action_PermissionKey] ON [PageAccessPolicies] ([Area], [Controller], [Action], [PermissionKey]);
                    CREATE INDEX [IX_PageAccessPolicies_CreatorId] ON [PageAccessPolicies] ([CreatorId]);
                    CREATE INDEX [IX_PageAccessPolicies_UpdaterId] ON [PageAccessPolicies] ([UpdaterId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do nothing on rollback - we don't want to drop tables
        }
    }
}
