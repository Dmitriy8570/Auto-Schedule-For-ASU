using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkloadLogTimeStampIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WeekLogs_TimeStamp",
                table: "WeekLogs",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterLogs_TimeStamp",
                table: "SemesterLogs",
                column: "TimeStamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeekLogs_TimeStamp",
                table: "WeekLogs");

            migrationBuilder.DropIndex(
                name: "IX_SemesterLogs_TimeStamp",
                table: "SemesterLogs");
        }
    }
}
