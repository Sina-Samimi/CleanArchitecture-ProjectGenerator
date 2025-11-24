using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Smart migration: Only create tables if they don't exist
            
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tests')
                BEGIN
                    CREATE TABLE [Tests] (
                        [Id] uniqueidentifier NOT NULL,
                        [Title] nvarchar(300) NOT NULL,
                        [Description] nvarchar(max) NOT NULL,
                        [Type] int NOT NULL,
                        [Status] int NOT NULL,
                        [CategoryId] uniqueidentifier NULL,
                        [Price] decimal(18,2) NOT NULL DEFAULT 0,
                        [DurationMinutes] int NULL,
                        [MaxAttempts] int NULL,
                        [ShowResultsImmediately] bit NOT NULL DEFAULT 1,
                        [ShowCorrectAnswers] bit NOT NULL DEFAULT 0,
                        [RandomizeQuestions] bit NOT NULL DEFAULT 0,
                        [RandomizeOptions] bit NOT NULL DEFAULT 0,
                        [AvailableFrom] datetimeoffset NULL,
                        [AvailableUntil] datetimeoffset NULL,
                        [NumberOfQuestionsToShow] int NULL,
                        [PassingScore] decimal(18,2) NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        CONSTRAINT [PK_Tests] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Tests_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_Tests_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_Tests_SiteCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [SiteCategories] ([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_Tests_CategoryId] ON [Tests] ([CategoryId]);
                    CREATE INDEX [IX_Tests_CreateDate] ON [Tests] ([CreateDate]);
                    CREATE INDEX [IX_Tests_CreatorId] ON [Tests] ([CreatorId]);
                    CREATE INDEX [IX_Tests_Status] ON [Tests] ([Status]);
                    CREATE INDEX [IX_Tests_UpdaterId] ON [Tests] ([UpdaterId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestQuestions')
                BEGIN
                    CREATE TABLE [TestQuestions] (
                        [Id] uniqueidentifier NOT NULL,
                        [TestId] uniqueidentifier NOT NULL,
                        [Text] nvarchar(max) NOT NULL,
                        [QuestionType] int NOT NULL,
                        [Order] int NOT NULL,
                        [Score] int NULL,
                        [IsRequired] bit NOT NULL DEFAULT 1,
                        [ImageUrl] nvarchar(600) NULL,
                        [Explanation] nvarchar(max) NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        CONSTRAINT [PK_TestQuestions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_TestQuestions_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_TestQuestions_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_TestQuestions_Tests_TestId] FOREIGN KEY ([TestId]) REFERENCES [Tests] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_TestQuestions_CreatorId] ON [TestQuestions] ([CreatorId]);
                    CREATE INDEX [IX_TestQuestions_Order] ON [TestQuestions] ([Order]);
                    CREATE INDEX [IX_TestQuestions_TestId] ON [TestQuestions] ([TestId]);
                    CREATE INDEX [IX_TestQuestions_UpdaterId] ON [TestQuestions] ([UpdaterId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestQuestionOptions')
                BEGIN
                    CREATE TABLE [TestQuestionOptions] (
                        [Id] uniqueidentifier NOT NULL,
                        [QuestionId] uniqueidentifier NOT NULL,
                        [Text] nvarchar(max) NOT NULL,
                        [IsCorrect] bit NOT NULL,
                        [Score] int NULL,
                        [ImageUrl] nvarchar(600) NULL,
                        [Explanation] nvarchar(max) NULL,
                        [Order] int NOT NULL,
                        [Ip] nvarchar(64) NOT NULL,
                        [CreatorId] nvarchar(450) NOT NULL,
                        [UpdaterId] nvarchar(450) NULL,
                        [CreateDate] datetimeoffset NOT NULL,
                        [IsDeleted] bit NOT NULL,
                        [UpdateDate] datetimeoffset NOT NULL,
                        [RemoveDate] datetimeoffset NULL,
                        CONSTRAINT [PK_TestQuestionOptions] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_TestQuestionOptions_AspNetUsers_CreatorId] FOREIGN KEY ([CreatorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_TestQuestionOptions_AspNetUsers_UpdaterId] FOREIGN KEY ([UpdaterId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_TestQuestionOptions_TestQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [TestQuestions] ([Id]) ON DELETE CASCADE
                    );

                    CREATE INDEX [IX_TestQuestionOptions_CreatorId] ON [TestQuestionOptions] ([CreatorId]);
                    CREATE INDEX [IX_TestQuestionOptions_Order] ON [TestQuestionOptions] ([Order]);
                    CREATE INDEX [IX_TestQuestionOptions_QuestionId] ON [TestQuestionOptions] ([QuestionId]);
                    CREATE INDEX [IX_TestQuestionOptions_UpdaterId] ON [TestQuestionOptions] ([UpdaterId]);
                END
            ");

            // Add Explanation column to existing TestQuestionOptions table if it doesn't have it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestQuestionOptions')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TestQuestionOptions') AND name = 'Explanation')
                    BEGIN
                        ALTER TABLE [TestQuestionOptions] ADD [Explanation] nvarchar(max) NULL;
                    END
                END
            ");

            // Add CategoryId column to existing Tests table if it doesn't have it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Tests')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tests') AND name = 'CategoryId')
                    BEGIN
                        ALTER TABLE [Tests] ADD [CategoryId] uniqueidentifier NULL;
                        
                        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Tests_SiteCategories_CategoryId')
                        BEGIN
                            ALTER TABLE [Tests] ADD CONSTRAINT [FK_Tests_SiteCategories_CategoryId] 
                                FOREIGN KEY ([CategoryId]) REFERENCES [SiteCategories] ([Id]) ON DELETE NO ACTION;
                        END
                        
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tests_CategoryId' AND object_id = OBJECT_ID('Tests'))
                        BEGIN
                            CREATE INDEX [IX_Tests_CategoryId] ON [Tests] ([CategoryId]);
                        END
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TestQuestionOptions') AND name = 'Explanation')
                BEGIN
                    ALTER TABLE [TestQuestionOptions] DROP COLUMN [Explanation];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tests') AND name = 'CategoryId')
                BEGIN
                    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Tests_SiteCategories_CategoryId')
                    BEGIN
                        ALTER TABLE [Tests] DROP CONSTRAINT [FK_Tests_SiteCategories_CategoryId];
                    END
                    
                    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tests_CategoryId' AND object_id = OBJECT_ID('Tests'))
                    BEGIN
                        DROP INDEX [IX_Tests_CategoryId] ON [Tests];
                    END
                    
                    ALTER TABLE [Tests] DROP COLUMN [CategoryId];
                END
            ");

            migrationBuilder.DropTable(
                name: "TestQuestionOptions");

            migrationBuilder.DropTable(
                name: "TestQuestions");

            migrationBuilder.DropTable(
                name: "Tests");
        }
    }
}
