using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Services.Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "BuyerId",
                schema: "ordering",
                table: "Orders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "ordering",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerId_CreatedDate",
                schema: "ordering",
                table: "Orders",
                columns: new[] { "BuyerId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderStatus_CreatedDate",
                schema: "ordering",
                table: "Orders",
                columns: new[] { "OrderStatus", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_BuyerId_CreatedDate",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderStatus_CreatedDate",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "BuyerId",
                schema: "ordering",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
