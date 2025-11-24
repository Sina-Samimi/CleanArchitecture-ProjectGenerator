using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupportPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdaterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RemoveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteSettings_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteSettings_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_CreatorId",
                table: "SiteSettings",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_UpdaterId",
                table: "SiteSettings",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettings");
        }
    }
}
