using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NavigationMenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    OpenInNewTab = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_NavigationMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_NavigationMenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "NavigationMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_CreatorId",
                table: "NavigationMenuItems",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_ParentId",
                table: "NavigationMenuItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_ParentId_DisplayOrder",
                table: "NavigationMenuItems",
                columns: new[] { "ParentId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_UpdaterId",
                table: "NavigationMenuItems",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavigationMenuItems");
        }
    }
}
