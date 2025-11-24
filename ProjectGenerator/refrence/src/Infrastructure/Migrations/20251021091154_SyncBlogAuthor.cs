using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncBlogAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[PageAccessPolicies]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PageAccessPolicies_AspNetUsers_UserId'
                    )
                    BEGIN
                        ALTER TABLE [PageAccessPolicies] DROP CONSTRAINT [FK_PageAccessPolicies_AspNetUsers_UserId];
                    END

                    IF COL_LENGTH('PageAccessPolicies', 'UserId') IS NOT NULL
                    BEGIN
                        ALTER TABLE [PageAccessPolicies] DROP COLUMN [UserId];
                    END
                END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[PageAccessPolicies]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('PageAccessPolicies', 'UserId') IS NULL
                    BEGIN
                        ALTER TABLE [PageAccessPolicies] ADD [UserId] nvarchar(450) NULL;
                    END

                    IF COL_LENGTH('PageAccessPolicies', 'UserId') IS NOT NULL AND NOT EXISTS (
                        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PageAccessPolicies_AspNetUsers_UserId'
                    )
                    BEGIN
                        ALTER TABLE [PageAccessPolicies] ADD CONSTRAINT [FK_PageAccessPolicies_AspNetUsers_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION;
                    END
                END
            """);
        }
    }
}
