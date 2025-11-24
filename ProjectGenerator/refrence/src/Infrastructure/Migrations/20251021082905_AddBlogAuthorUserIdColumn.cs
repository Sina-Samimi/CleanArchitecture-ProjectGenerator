using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogAuthorUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PageAccessPolicies_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [PageAccessPolicies] DROP CONSTRAINT [FK_PageAccessPolicies_AspNetUsers_UserId];
                END
            """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('PageAccessPolicies', 'UserId') IS NOT NULL
                BEGIN
                    ALTER TABLE [PageAccessPolicies] DROP COLUMN [UserId];
                END
            """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[BlogAuthors]', N'U') IS NOT NULL AND COL_LENGTH('BlogAuthors', 'UserId') IS NULL
                BEGIN
                    ALTER TABLE [BlogAuthors] ADD [UserId] nvarchar(450) NULL;
                END
            """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[BlogAuthors]', N'U') IS NOT NULL AND COL_LENGTH('BlogAuthors', 'UserId') IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM sys.indexes WHERE name = 'IX_BlogAuthors_UserId' AND object_id = OBJECT_ID('[BlogAuthors]')
                )
                BEGIN
                    CREATE INDEX [IX_BlogAuthors_UserId] ON [BlogAuthors]([UserId]);
                END
            """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[BlogAuthors]', N'U') IS NOT NULL AND COL_LENGTH('BlogAuthors', 'UserId') IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BlogAuthors_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [BlogAuthors] ADD CONSTRAINT [FK_BlogAuthors_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION;
                END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_BlogAuthors_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [BlogAuthors] DROP CONSTRAINT [FK_BlogAuthors_AspNetUsers_UserId];
                END
            """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes WHERE name = 'IX_BlogAuthors_UserId' AND object_id = OBJECT_ID('[BlogAuthors]')
                )
                BEGIN
                    DROP INDEX [IX_BlogAuthors_UserId] ON [BlogAuthors];
                END
            """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('BlogAuthors', 'UserId') IS NOT NULL
                BEGIN
                    ALTER TABLE [BlogAuthors] DROP COLUMN [UserId];
                END
            """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('PageAccessPolicies', 'UserId') IS NULL
                BEGIN
                    ALTER TABLE [PageAccessPolicies] ADD [UserId] nvarchar(450) NULL;
                END
            """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('PageAccessPolicies', 'UserId') IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PageAccessPolicies_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [PageAccessPolicies] ADD CONSTRAINT [FK_PageAccessPolicies_AspNetUsers_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION;
                END
            """);
        }
    }
}
