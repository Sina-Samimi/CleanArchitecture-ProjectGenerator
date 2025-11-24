using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyToTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Tests')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tests') AND name = 'Currency')
                    BEGIN
                        ALTER TABLE [Tests] ADD [Currency] nvarchar(10) NOT NULL DEFAULT 'IRT';
                    END
                    ELSE
                    BEGIN
                        -- Update existing NULL values to IRT
                        UPDATE [Tests] SET [Currency] = 'IRT' WHERE [Currency] IS NULL;
                        -- Make it NOT NULL if it's nullable
                        ALTER TABLE [Tests] ALTER COLUMN [Currency] nvarchar(10) NOT NULL;
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tests') AND name = 'Currency')
                BEGIN
                    ALTER TABLE [Tests] DROP COLUMN [Currency];
                END
            ");
        }
    }
}
