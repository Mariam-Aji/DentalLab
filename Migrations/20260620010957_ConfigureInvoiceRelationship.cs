using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalLab.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureInvoiceRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseOrderId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    MyFatoorahInvoiceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderInvoices_CaseOrders_CaseOrderId",
                        column: x => x.CaseOrderId,
                        principalTable: "CaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderInvoices_CaseOrderId",
                table: "OrderInvoices",
                column: "CaseOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderInvoices");
        }
    }
}
