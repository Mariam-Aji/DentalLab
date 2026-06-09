using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLab.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogPostIdToNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Notifications");

            migrationBuilder.AddColumn<int>(
                name: "BlogPostId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Target",
                table: "Advertisements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BlogPostId",
                table: "Notifications",
                column: "BlogPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_BlogPosts_BlogPostId",
                table: "Notifications",
                column: "BlogPostId",
                principalTable: "BlogPosts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_BlogPosts_BlogPostId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BlogPostId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BlogPostId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "Advertisements");

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
