using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Services.Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCorrelationIdAndOrderState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill: NEWID() varsayılanı mevcut her satır için ayrı GUID üretir (EF'in sabit 0000... değeri
            // tüm eski satırlara aynı değeri verir ve aşağıdaki unique index'i kırardı).
            // Eski siparişlerin orijinal correlation id'leri hiçbir tabloda tutulmadığı için geri getirilemez.
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                schema: "ordering",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // Default kaldırılır: bundan sonra değeri yalnızca uygulama atar; atanmazsa hata görünür olur.
            migrationBuilder.AlterColumn<Guid>(
                name: "CorrelationId",
                schema: "ordering",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWID()");

            migrationBuilder.AlterColumn<string>(
                name: "OperationName",
                schema: "ordering",
                table: "IdempotentRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "OrderStates",
                schema: "ordering",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTimeoutTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStates", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CorrelationId",
                schema: "ordering",
                table: "Orders",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderStates_CurrentState_ExpiresAt",
                schema: "ordering",
                table: "OrderStates",
                columns: new[] { "CurrentState", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStates_OrderId",
                schema: "ordering",
                table: "OrderStates",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderStates",
                schema: "ordering");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CorrelationId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "OperationName",
                schema: "ordering",
                table: "IdempotentRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
