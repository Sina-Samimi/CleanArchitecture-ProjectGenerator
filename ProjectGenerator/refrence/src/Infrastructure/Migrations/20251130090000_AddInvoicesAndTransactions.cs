using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicesAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Invoices]
    (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceNumber] nvarchar(64) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [UserId] nvarchar(450) NULL,
        [Currency] nvarchar(16) NOT NULL,
        [Status] int NOT NULL,
        [IssueDate] datetimeoffset NOT NULL,
        [DueDate] datetimeoffset NULL,
        [TaxAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Invoices_TaxAmount] DEFAULT ((0)),
        [AdjustmentAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Invoices_AdjustmentAmount] DEFAULT ((0)),
        [ExternalReference] nvarchar(128) NULL,
        [CreateDate] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        [UpdateDate] datetimeoffset NOT NULL,
        [RemoveDate] datetimeoffset NULL,
        [Ip] nvarchar(64) NOT NULL,
        [CreatorId] nvarchar(450) NOT NULL,
        [UpdaterId] nvarchar(450) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Invoices_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_CreatorId' AND object_id = OBJECT_ID(N'[dbo].[Invoices]'))
BEGIN
    CREATE INDEX [IX_Invoices_CreatorId] ON [dbo].[Invoices]([CreatorId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_InvoiceNumber' AND object_id = OBJECT_ID(N'[dbo].[Invoices]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [dbo].[Invoices]([InvoiceNumber]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_UpdaterId' AND object_id = OBJECT_ID(N'[dbo].[Invoices]'))
BEGIN
    CREATE INDEX [IX_Invoices_UpdaterId] ON [dbo].[Invoices]([UpdaterId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_UserId' AND object_id = OBJECT_ID(N'[dbo].[Invoices]'))
BEGIN
    CREATE INDEX [IX_Invoices_UserId] ON [dbo].[Invoices]([UserId]);
END;

IF OBJECT_ID(N'[dbo].[InvoiceItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InvoiceItems]
    (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ItemType] int NOT NULL,
        [ReferenceId] uniqueidentifier NULL,
        [Quantity] decimal(18,2) NOT NULL CONSTRAINT [DF_InvoiceItems_Quantity] DEFAULT ((1)),
        [UnitPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_InvoiceItems_UnitPrice] DEFAULT ((0)),
        [DiscountAmount] decimal(18,2) NULL,
        [CreateDate] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        [UpdateDate] datetimeoffset NOT NULL,
        [RemoveDate] datetimeoffset NULL,
        [Ip] nvarchar(64) NOT NULL,
        [CreatorId] nvarchar(450) NOT NULL,
        [UpdaterId] nvarchar(450) NULL,
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InvoiceItems_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItems_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItems_CreatorId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItems]'))
BEGIN
    CREATE INDEX [IX_InvoiceItems_CreatorId] ON [dbo].[InvoiceItems]([CreatorId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItems_InvoiceId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItems]'))
BEGIN
    CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [dbo].[InvoiceItems]([InvoiceId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItems_UpdaterId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItems]'))
BEGIN
    CREATE INDEX [IX_InvoiceItems_UpdaterId] ON [dbo].[InvoiceItems]([UpdaterId]);
END;

IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PaymentTransactions]
    (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Method] int NOT NULL,
        [Status] int NOT NULL,
        [Reference] nvarchar(100) NOT NULL,
        [GatewayName] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [Metadata] nvarchar(2000) NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        [CreateDate] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        [UpdateDate] datetimeoffset NOT NULL,
        [RemoveDate] datetimeoffset NULL,
        [Ip] nvarchar(64) NOT NULL,
        [CreatorId] nvarchar(450) NOT NULL,
        [UpdaterId] nvarchar(450) NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PaymentTransactions_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentTransactions_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentTransactions_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_CreatorId' AND object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_CreatorId] ON [dbo].[PaymentTransactions]([CreatorId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_InvoiceId' AND object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_InvoiceId] ON [dbo].[PaymentTransactions]([InvoiceId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentTransactions_UpdaterId' AND object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]'))
BEGIN
    CREATE INDEX [IX_PaymentTransactions_UpdaterId] ON [dbo].[PaymentTransactions]([UpdaterId]);
END;

IF OBJECT_ID(N'[dbo].[InvoiceItemAttributes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InvoiceItemAttributes]
    (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceItemId] uniqueidentifier NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(1000) NOT NULL,
        [CreateDate] datetimeoffset NOT NULL,
        [IsDeleted] bit NOT NULL,
        [UpdateDate] datetimeoffset NOT NULL,
        [RemoveDate] datetimeoffset NULL,
        [Ip] nvarchar(64) NOT NULL,
        [CreatorId] nvarchar(450) NOT NULL,
        [UpdaterId] nvarchar(450) NULL,
        CONSTRAINT [PK_InvoiceItemAttributes] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InvoiceItemAttributes_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItemAttributes_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItemAttributes_InvoiceItems_InvoiceItemId] FOREIGN KEY ([InvoiceItemId]) REFERENCES [dbo].[InvoiceItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItemAttributes_CreatorId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItemAttributes]'))
BEGIN
    CREATE INDEX [IX_InvoiceItemAttributes_CreatorId] ON [dbo].[InvoiceItemAttributes]([CreatorId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItemAttributes_InvoiceItemId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItemAttributes]'))
BEGIN
    CREATE INDEX [IX_InvoiceItemAttributes_InvoiceItemId] ON [dbo].[InvoiceItemAttributes]([InvoiceItemId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItemAttributes_UpdaterId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItemAttributes]'))
BEGIN
    CREATE INDEX [IX_InvoiceItemAttributes_UpdaterId] ON [dbo].[InvoiceItemAttributes]([UpdaterId]);
END;
");
            }
            else
            {
                migrationBuilder.CreateTable(
                    name: "Invoices",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                        Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                        UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                        Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                        Status = table.Column<int>(type: "int", nullable: false),
                        IssueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                        AdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                        ExternalReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                        CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                        UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                        CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                        UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Invoices", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Invoices_AspNetUsers_CreatorId",
                            column: x => x.CreatorId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_Invoices_AspNetUsers_UpdaterId",
                            column: x => x.UpdaterId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                    });

                migrationBuilder.CreateTable(
                    name: "InvoiceItems",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                        Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                        ItemType = table.Column<int>(type: "int", nullable: false),
                        ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                        Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 1m),
                        UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                        DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                        CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                        UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                        CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                        UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                        table.ForeignKey(
                            name: "FK_InvoiceItems_AspNetUsers_CreatorId",
                            column: x => x.CreatorId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_InvoiceItems_AspNetUsers_UpdaterId",
                            column: x => x.UpdaterId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_InvoiceItems_Invoices_InvoiceId",
                            column: x => x.InvoiceId,
                            principalTable: "Invoices",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    name: "PaymentTransactions",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                        Method = table.Column<int>(type: "int", nullable: false),
                        Status = table.Column<int>(type: "int", nullable: false),
                        Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                        GatewayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                        Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                        Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                        OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                        UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                        CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                        UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                        table.ForeignKey(
                            name: "FK_PaymentTransactions_AspNetUsers_CreatorId",
                            column: x => x.CreatorId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_PaymentTransactions_AspNetUsers_UpdaterId",
                            column: x => x.UpdaterId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_PaymentTransactions_Invoices_InvoiceId",
                            column: x => x.InvoiceId,
                            principalTable: "Invoices",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateTable(
                    name: "InvoiceItemAttributes",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        InvoiceItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                        Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                        Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                        CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                        UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                        Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                        CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                        UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_InvoiceItemAttributes", x => x.Id);
                        table.ForeignKey(
                            name: "FK_InvoiceItemAttributes_AspNetUsers_CreatorId",
                            column: x => x.CreatorId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_InvoiceItemAttributes_AspNetUsers_UpdaterId",
                            column: x => x.UpdaterId,
                            principalTable: "AspNetUsers",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Restrict);
                        table.ForeignKey(
                            name: "FK_InvoiceItemAttributes_InvoiceItems_InvoiceItemId",
                            column: x => x.InvoiceItemId,
                            principalTable: "InvoiceItems",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateIndex(
                    name: "IX_InvoiceItemAttributes_CreatorId",
                    table: "InvoiceItemAttributes",
                    column: "CreatorId");

                migrationBuilder.CreateIndex(
                    name: "IX_InvoiceItemAttributes_InvoiceItemId",
                    table: "InvoiceItemAttributes",
                    column: "InvoiceItemId");

                migrationBuilder.CreateIndex(
                    name: "IX_InvoiceItemAttributes_UpdaterId",
                    table: "InvoiceItemAttributes",
                    column: "UpdaterId");

                migrationBuilder.CreateIndex(
                    name: "IX_InvoiceItems_CreatorId",
                    table: "InvoiceItems",
                    column: "CreatorId");

                migrationBuilder.CreateIndex(
                    name: "IX_InvoiceItems_InvoiceId",
                    table: "InvoiceItems",
                    column: "InvoiceId");

                migrationBuilder.CreateIndex(
                    name: "IX_InvoiceItems_UpdaterId",
                    table: "InvoiceItems",
                    column: "UpdaterId");

                migrationBuilder.CreateIndex(
                    name: "IX_Invoices_CreatorId",
                    table: "Invoices",
                    column: "CreatorId");

                migrationBuilder.CreateIndex(
                    name: "IX_Invoices_InvoiceNumber",
                    table: "Invoices",
                    column: "InvoiceNumber",
                    unique: true);

                migrationBuilder.CreateIndex(
                    name: "IX_Invoices_UpdaterId",
                    table: "Invoices",
                    column: "UpdaterId");

                migrationBuilder.CreateIndex(
                    name: "IX_Invoices_UserId",
                    table: "Invoices",
                    column: "UserId");

                migrationBuilder.CreateIndex(
                    name: "IX_PaymentTransactions_CreatorId",
                    table: "PaymentTransactions",
                    column: "CreatorId");

                migrationBuilder.CreateIndex(
                    name: "IX_PaymentTransactions_InvoiceId",
                    table: "PaymentTransactions",
                    column: "InvoiceId");

                migrationBuilder.CreateIndex(
                    name: "IX_PaymentTransactions_UpdaterId",
                    table: "PaymentTransactions",
                    column: "UpdaterId");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InvoiceItemAttributes]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[InvoiceItemAttributes];
END;

IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[PaymentTransactions];
END;

IF OBJECT_ID(N'[dbo].[InvoiceItems]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[InvoiceItems];
END;

IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[Invoices];
END;
");
            }
            else
            {
                migrationBuilder.DropTable(
                    name: "InvoiceItemAttributes");

                migrationBuilder.DropTable(
                    name: "PaymentTransactions");

                migrationBuilder.DropTable(
                    name: "InvoiceItems");

                migrationBuilder.DropTable(
                    name: "Invoices");
            }
        }
    }
}
