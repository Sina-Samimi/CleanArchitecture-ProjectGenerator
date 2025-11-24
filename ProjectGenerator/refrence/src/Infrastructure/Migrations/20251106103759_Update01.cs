using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    MaxAttempts = table.Column<int>(type: "int", nullable: true),
                    ShowResultsImmediately = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowCorrectAnswers = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RandomizeQuestions = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RandomizeOptions = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AvailableFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AvailableUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NumberOfQuestionsToShow = table.Column<int>(type: "int", nullable: true),
                    PassingScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_Tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tests_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tests_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tests_SiteCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SiteCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TestQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestQuestions_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestQuestions_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestQuestions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTestAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ScorePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsPassed = table.Column<bool>(type: "bit", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_UserTestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTestAttempts_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAttempts_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAttempts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAttempts_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TestQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestQuestionOptions_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestQuestionOptions_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestQuestionOptions_TestQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "TestQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestResults_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestResults_UserTestAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "UserTestAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserTestAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TextAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LikertValue = table.Column<int>(type: "int", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_UserTestAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTestAnswers_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAnswers_AspNetUsers_UpdaterId",
                        column: x => x.UpdaterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAnswers_TestQuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "TestQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAnswers_TestQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "TestQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTestAnswers_UserTestAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "UserTestAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionOptions_CreatorId",
                table: "TestQuestionOptions",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionOptions_Order",
                table: "TestQuestionOptions",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionOptions_QuestionId",
                table: "TestQuestionOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionOptions_UpdaterId",
                table: "TestQuestionOptions",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_CreatorId",
                table: "TestQuestions",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_Order",
                table: "TestQuestions",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_TestId",
                table: "TestQuestions",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_UpdaterId",
                table: "TestQuestions",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_AttemptId",
                table: "TestResults",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CreatorId",
                table: "TestResults",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_ResultType",
                table: "TestResults",
                column: "ResultType");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_UpdaterId",
                table: "TestResults",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_CategoryId",
                table: "Tests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_CreateDate",
                table: "Tests",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_CreatorId",
                table: "Tests",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_Status",
                table: "Tests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_UpdaterId",
                table: "Tests",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAnswers_AttemptId",
                table: "UserTestAnswers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAnswers_AttemptId_QuestionId",
                table: "UserTestAnswers",
                columns: new[] { "AttemptId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAnswers_CreatorId",
                table: "UserTestAnswers",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAnswers_QuestionId",
                table: "UserTestAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAnswers_SelectedOptionId",
                table: "UserTestAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAnswers_UpdaterId",
                table: "UserTestAnswers",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAttempts_CreatorId",
                table: "UserTestAttempts",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAttempts_Status",
                table: "UserTestAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAttempts_TestId",
                table: "UserTestAttempts",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAttempts_TestId_UserId_AttemptNumber",
                table: "UserTestAttempts",
                columns: new[] { "TestId", "UserId", "AttemptNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAttempts_UpdaterId",
                table: "UserTestAttempts",
                column: "UpdaterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTestAttempts_UserId",
                table: "UserTestAttempts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "UserTestAnswers");

            migrationBuilder.DropTable(
                name: "TestQuestionOptions");

            migrationBuilder.DropTable(
                name: "UserTestAttempts");

            migrationBuilder.DropTable(
                name: "TestQuestions");

            migrationBuilder.DropTable(
                name: "Tests");
        }
    }
}
