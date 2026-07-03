using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLab.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureInvoiceTableAndPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderInvoices_CaseOrderId",
                table: "OrderInvoices");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "OrderInvoices");

            migrationBuilder.DropColumn(
                name: "MyFatoorahInvoiceId",
                table: "OrderInvoices");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "OrderInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentUrl",
                table: "OrderInvoices");

            migrationBuilder.AlterColumn<int>(
                name: "CaseOrderId",
                table: "OrderInvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "OrderInvoiceItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderInvoiceId = table.Column<int>(type: "int", nullable: false),
                    CompensationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToothNumbers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TeethCount = table.Column<int>(type: "int", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInvoiceItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderInvoiceItem_OrderInvoices_OrderInvoiceId",
                        column: x => x.OrderInvoiceId,
                        principalTable: "OrderInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoices_CaseOrderId",
                table: "OrderInvoices",
                column: "CaseOrderId",
                unique: true,
                filter: "[CaseOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoiceItem_OrderInvoiceId",
                table: "OrderInvoiceItem",
                column: "OrderInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderInvoiceItem");

            migrationBuilder.DropIndex(
                name: "IX_OrderInvoices_CaseOrderId",
                table: "OrderInvoices");

            migrationBuilder.AlterColumn<int>(
                name: "CaseOrderId",
                table: "OrderInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "OrderInvoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MyFatoorahInvoiceId",
                table: "OrderInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "OrderInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentUrl",
                table: "OrderInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoices_CaseOrderId",
                table: "OrderInvoices",
                column: "CaseOrderId",
                unique: true);
        }
    }
}
