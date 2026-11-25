using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Services.Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                schema: "ordering",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 1); // PaymentStatus.Alinmadi

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                schema: "ordering",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            // Mevcut kayıtlar: tamamlanmış siparişin ödemesi alınmıştır. Eski iadelerin bilgisi Payment'ta kalır.
            migrationBuilder.Sql("UPDATE ordering.Orders SET PaymentStatus = 2 WHERE OrderStatus = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                schema: "ordering",
                table: "Orders");
        }
    }
}
