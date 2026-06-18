using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonSemesterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "Lessons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SemesterId",
                table: "Lessons",
                column: "SemesterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Semesters_SemesterId",
                table: "Lessons",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Semesters_SemesterId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_SemesterId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "Lessons");
        }
    }
}
