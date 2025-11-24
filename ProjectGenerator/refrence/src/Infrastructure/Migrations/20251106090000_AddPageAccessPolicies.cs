using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPageAccessPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
            migrationBuilder.DropTable(
                name: "PageAccessPolicies");
        }
    }
}
