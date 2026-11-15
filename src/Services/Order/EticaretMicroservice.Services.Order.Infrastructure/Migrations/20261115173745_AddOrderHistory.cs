using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Services.Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderHistory",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderHistory_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistory_OrderId",
                schema: "ordering",
                table: "OrderHistory",
                column: "OrderId");

            // Mevcut siparişler: zamanı bilinen adımlar (1 = SiparisAlindi, 8 = IadeEdildi). Diğer adımların zamanı kayıtlı değil.
            migrationBuilder.Sql(@"
INSERT INTO ordering.OrderHistory (OrderId, Type, OccurredAt) SELECT Id, 1, CreatedDate FROM ordering.Orders;
INSERT INTO ordering.OrderHistory (OrderId, Type, OccurredAt) SELECT Id, 8, RefundedAt FROM ordering.Orders WHERE RefundedAt IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderHistory",
                schema: "ordering");
        }
    }
}
