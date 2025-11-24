using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeploymentProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeploymentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ServerHost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServerPort = table.Column<int>(type: "int", nullable: false),
                    ServerUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DestinationPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ArtifactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreDeployCommand = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PostDeployCommand = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ServiceReloadCommand = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SecretKeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_DeploymentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentProfiles_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeploymentProfiles_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentProfiles_Branch",
                table: "DeploymentProfiles",
                column: "Branch",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentProfiles_CreatorId",
                table: "DeploymentProfiles",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentProfiles_Name",
                table: "DeploymentProfiles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentProfiles_UpdaterId",
                table: "DeploymentProfiles",
                column: "UpdaterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentProfiles");
        }
    }
}
