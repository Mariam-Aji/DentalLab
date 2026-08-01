using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLab.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintReplyField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RepliedAtUtc",
                table: "Complaints",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reply",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepliedAtUtc",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "Reply",
                table: "Complaints");
        }
    }
}
