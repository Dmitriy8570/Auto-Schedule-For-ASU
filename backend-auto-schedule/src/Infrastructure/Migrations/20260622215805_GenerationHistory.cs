using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GenerationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GenerationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    InstituteId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstituteName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LessonsCreated = table.Column<int>(type: "integer", nullable: false),
                    ObjectiveValue = table.Column<double>(type: "double precision", nullable: false),
                    WallTimeSeconds = table.Column<double>(type: "double precision", nullable: false),
                    UnplacedCount = table.Column<int>(type: "integer", nullable: false),
                    UnplacedJson = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRuns_CompletedAt",
                table: "GenerationRuns",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationRuns_SemesterId_InstituteId",
                table: "GenerationRuns",
                columns: new[] { "SemesterId", "InstituteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenerationRuns");
        }
    }
}
