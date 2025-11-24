using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create WalletAccounts table only if it doesn't exist
            migrationBuilder.Sql(@"
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
            ");

            // Create WalletTransactions table only if it doesn't exist
            migrationBuilder.Sql(@"
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
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "WalletAccounts");
        }
    }
}
