using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MmisSyncLogsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SemesterLogs_SemesterWorkloads_SemesterWorkloadId",
                table: "SemesterLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WeekLogs_WeekWorkloads_WeekWorkloadId",
                table: "WeekLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "WeekWorkloadId",
                table: "WeekLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SemesterWorkloadId",
                table: "SemesterLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_SemesterLogs_SemesterWorkloads_SemesterWorkloadId",
                table: "SemesterLogs",
                column: "SemesterWorkloadId",
                principalTable: "SemesterWorkloads",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WeekLogs_WeekWorkloads_WeekWorkloadId",
                table: "WeekLogs",
                column: "WeekWorkloadId",
                principalTable: "WeekWorkloads",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SemesterLogs_SemesterWorkloads_SemesterWorkloadId",
                table: "SemesterLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WeekLogs_WeekWorkloads_WeekWorkloadId",
                table: "WeekLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "WeekWorkloadId",
                table: "WeekLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SemesterWorkloadId",
                table: "SemesterLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SemesterLogs_SemesterWorkloads_SemesterWorkloadId",
                table: "SemesterLogs",
                column: "SemesterWorkloadId",
                principalTable: "SemesterWorkloads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeekLogs_WeekWorkloads_WeekWorkloadId",
                table: "WeekLogs",
                column: "WeekWorkloadId",
                principalTable: "WeekWorkloads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
