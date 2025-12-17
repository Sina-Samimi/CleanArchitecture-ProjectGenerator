using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Author role if it doesn't exist (check both Id and NormalizedName to avoid duplicate key error)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Id = 'd4e5f6a7-b8c9-7d8e-1f2a-3b4c5d6e7f8a' OR NormalizedName = 'AUTHOR')
                BEGIN
                    INSERT INTO AspNetRoles (Id, ConcurrencyStamp, Name, NormalizedName)
                    VALUES ('d4e5f6a7-b8c9-7d8e-1f2a-3b4c5d6e7f8a', 'AUTHOR-CONCURRENCY-STAMP-2024', 'Author', 'AUTHOR');
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Author role
            migrationBuilder.Sql(@"
                DELETE FROM AspNetRoles WHERE Id = 'd4e5f6a7-b8c9-7d8e-1f2a-3b4c5d6e7f8a';
            ");
        }
    }
}
