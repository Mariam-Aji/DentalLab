using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLab.Api.Migrations
{
    /// <inheritdoc />
    public partial class Complaint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Complaints",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "AdminId",
                table: "Complaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Complaints",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TargetLabId",
                table: "Complaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Complaints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AdminId",
                table: "Complaints",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_TargetLabId",
                table: "Complaints",
                column: "TargetLabId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_UserId",
                table: "Complaints",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Labs_TargetLabId",
                table: "Complaints",
                column: "TargetLabId",
                principalTable: "Labs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Users_AdminId",
                table: "Complaints",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Users_UserId",
                table: "Complaints",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Labs_TargetLabId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Users_AdminId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Users_UserId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_AdminId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_TargetLabId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_UserId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "TargetLabId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Complaints");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
