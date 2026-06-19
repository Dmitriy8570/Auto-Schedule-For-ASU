using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCurriculumId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumId",
                table: "Lessons",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CurriculumId",
                table: "Lessons",
                column: "CurriculumId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Curriculums_CurriculumId",
                table: "Lessons",
                column: "CurriculumId",
                principalTable: "Curriculums",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Curriculums_CurriculumId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CurriculumId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "Lessons");
        }
    }
}
