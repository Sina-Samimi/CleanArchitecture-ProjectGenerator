using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixInvoiceNavigationOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Invoices]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [InvoiceNumber] NVARCHAR(64) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [UserId] NVARCHAR(450) NULL,
        [Currency] NVARCHAR(16) NOT NULL,
        [Status] INT NOT NULL,
        [IssueDate] DATETIMEOFFSET NOT NULL,
        [DueDate] DATETIMEOFFSET NULL,
        [TaxAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Invoices_TaxAmount] DEFAULT ((0)),
        [AdjustmentAmount] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_Invoices_AdjustmentAmount] DEFAULT ((0)),
        [ExternalReference] NVARCHAR(128) NULL,
        [CreateDate] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        [UpdateDate] DATETIMEOFFSET NOT NULL,
        [RemoveDate] DATETIMEOFFSET NULL,
        [Ip] NVARCHAR(64) NOT NULL,
        [CreatorId] NVARCHAR(450) NOT NULL,
        [UpdaterId] NVARCHAR(450) NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Invoices_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Invoices_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Invoices_CreatorId] ON [dbo].[Invoices]([CreatorId]);
    CREATE UNIQUE INDEX [IX_Invoices_InvoiceNumber] ON [dbo].[Invoices]([InvoiceNumber]);
    CREATE INDEX [IX_Invoices_UpdaterId] ON [dbo].[Invoices]([UpdaterId]);
    CREATE INDEX [IX_Invoices_UserId] ON [dbo].[Invoices]([UserId]);
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InvoiceItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InvoiceItems]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [ItemType] INT NOT NULL,
        [ReferenceId] UNIQUEIDENTIFIER NULL,
        [Quantity] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_InvoiceItems_Quantity] DEFAULT ((1)),
        [UnitPrice] DECIMAL(18,2) NOT NULL CONSTRAINT [DF_InvoiceItems_UnitPrice] DEFAULT ((0)),
        [DiscountAmount] DECIMAL(18,2) NULL,
        [CreateDate] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        [UpdateDate] DATETIMEOFFSET NOT NULL,
        [RemoveDate] DATETIMEOFFSET NULL,
        [Ip] NVARCHAR(64) NOT NULL,
        [CreatorId] NVARCHAR(450) NOT NULL,
        [UpdaterId] NVARCHAR(450) NULL,
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceItems_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItems_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItems_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_InvoiceItems_CreatorId] ON [dbo].[InvoiceItems]([CreatorId]);
    CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [dbo].[InvoiceItems]([InvoiceId]);
    CREATE INDEX [IX_InvoiceItems_UpdaterId] ON [dbo].[InvoiceItems]([UpdaterId]);
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[PaymentTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PaymentTransactions]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [InvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Method] INT NOT NULL,
        [Status] INT NOT NULL,
        [Reference] NVARCHAR(100) NOT NULL,
        [GatewayName] NVARCHAR(100) NULL,
        [Description] NVARCHAR(500) NULL,
        [Metadata] NVARCHAR(2000) NULL,
        [OccurredAt] DATETIMEOFFSET NOT NULL,
        [CreateDate] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        [UpdateDate] DATETIMEOFFSET NOT NULL,
        [RemoveDate] DATETIMEOFFSET NULL,
        [Ip] NVARCHAR(64) NOT NULL,
        [CreatorId] NVARCHAR(450) NOT NULL,
        [UpdaterId] NVARCHAR(450) NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentTransactions_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentTransactions_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaymentTransactions_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [dbo].[Invoices]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_PaymentTransactions_CreatorId] ON [dbo].[PaymentTransactions]([CreatorId]);
    CREATE INDEX [IX_PaymentTransactions_InvoiceId] ON [dbo].[PaymentTransactions]([InvoiceId]);
    CREATE INDEX [IX_PaymentTransactions_UpdaterId] ON [dbo].[PaymentTransactions]([UpdaterId]);
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InvoiceItemAttributes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InvoiceItemAttributes]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [InvoiceItemId] UNIQUEIDENTIFIER NOT NULL,
        [Key] NVARCHAR(100) NOT NULL,
        [Value] NVARCHAR(1000) NOT NULL,
        [CreateDate] DATETIMEOFFSET NOT NULL,
        [IsDeleted] BIT NOT NULL,
        [UpdateDate] DATETIMEOFFSET NOT NULL,
        [RemoveDate] DATETIMEOFFSET NULL,
        [Ip] NVARCHAR(64) NOT NULL,
        [CreatorId] NVARCHAR(450) NOT NULL,
        [UpdaterId] NVARCHAR(450) NULL,
        CONSTRAINT [PK_InvoiceItemAttributes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceItemAttributes_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItemAttributes_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceItemAttributes_InvoiceItems_InvoiceItemId] FOREIGN KEY ([InvoiceItemId]) REFERENCES [dbo].[InvoiceItems]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_InvoiceItemAttributes_CreatorId] ON [dbo].[InvoiceItemAttributes]([CreatorId]);
    CREATE INDEX [IX_InvoiceItemAttributes_InvoiceItemId] ON [dbo].[InvoiceItemAttributes]([InvoiceItemId]);
    CREATE INDEX [IX_InvoiceItemAttributes_UpdaterId] ON [dbo].[InvoiceItemAttributes]([UpdaterId]);
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NOT NULL
BEGIN
    DECLARE @dfTaxAmount NVARCHAR(128);
    SELECT @dfTaxAmount = dc.name
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Invoices]') AND c.name = N'TaxAmount';

    IF @dfTaxAmount IS NOT NULL
    BEGIN
        DECLARE @dropTaxAmountSql NVARCHAR(MAX) =
            N'ALTER TABLE [dbo].[Invoices] DROP CONSTRAINT [' + @dfTaxAmount + N']';
        EXEC(@dropTaxAmountSql);
    END;

    ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [DF_Invoices_TaxAmount] DEFAULT ((0)) FOR [TaxAmount];

    DECLARE @dfAdjustment NVARCHAR(128);
    SELECT @dfAdjustment = dc.name
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Invoices]') AND c.name = N'AdjustmentAmount';

    IF @dfAdjustment IS NOT NULL
    BEGIN
        DECLARE @dropAdjustmentSql NVARCHAR(MAX) =
            N'ALTER TABLE [dbo].[Invoices] DROP CONSTRAINT [' + @dfAdjustment + N']';
        EXEC(@dropAdjustmentSql);
    END;

    ALTER TABLE [dbo].[Invoices] ADD CONSTRAINT [DF_Invoices_AdjustmentAmount] DEFAULT ((0)) FOR [AdjustmentAmount];
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InvoiceItems]', N'U') IS NOT NULL
BEGIN
    DECLARE @dfQuantity NVARCHAR(128);
    SELECT @dfQuantity = dc.name
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[InvoiceItems]') AND c.name = N'Quantity';

    IF @dfQuantity IS NOT NULL
    BEGIN
        DECLARE @dropQuantitySql NVARCHAR(MAX) =
            N'ALTER TABLE [dbo].[InvoiceItems] DROP CONSTRAINT [' + @dfQuantity + N']';
        EXEC(@dropQuantitySql);
    END;

    ALTER TABLE [dbo].[InvoiceItems] ADD CONSTRAINT [DF_InvoiceItems_Quantity] DEFAULT ((1)) FOR [Quantity];

    DECLARE @dfUnitPrice NVARCHAR(128);
    SELECT @dfUnitPrice = dc.name
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[InvoiceItems]') AND c.name = N'UnitPrice';

    IF @dfUnitPrice IS NOT NULL
    BEGIN
        DECLARE @dropUnitPriceSql NVARCHAR(MAX) =
            N'ALTER TABLE [dbo].[InvoiceItems] DROP CONSTRAINT [' + @dfUnitPrice + N']';
        EXEC(@dropUnitPriceSql);
    END;

    ALTER TABLE [dbo].[InvoiceItems] ADD CONSTRAINT [DF_InvoiceItems_UnitPrice] DEFAULT ((0)) FOR [UnitPrice];
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InvoiceItems]', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'[dbo].[DF_InvoiceItems_Quantity]', N'D') IS NOT NULL
        ALTER TABLE [dbo].[InvoiceItems] DROP CONSTRAINT [DF_InvoiceItems_Quantity];

    IF OBJECT_ID(N'[dbo].[DF_InvoiceItems_UnitPrice]', N'D') IS NOT NULL
        ALTER TABLE [dbo].[InvoiceItems] DROP CONSTRAINT [DF_InvoiceItems_UnitPrice];
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'[dbo].[DF_Invoices_TaxAmount]', N'D') IS NOT NULL
        ALTER TABLE [dbo].[Invoices] DROP CONSTRAINT [DF_Invoices_TaxAmount];

    IF OBJECT_ID(N'[dbo].[DF_Invoices_AdjustmentAmount]', N'D') IS NOT NULL
        ALTER TABLE [dbo].[Invoices] DROP CONSTRAINT [DF_Invoices_AdjustmentAmount];
END;
");
        }
    }
}
