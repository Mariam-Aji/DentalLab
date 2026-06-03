using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLab.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToBlogPostTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BlogPosts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "BlogPosts");
        }
    }
}
