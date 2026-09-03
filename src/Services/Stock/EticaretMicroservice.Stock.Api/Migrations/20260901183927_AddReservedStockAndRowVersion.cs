using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Stock.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservedStockAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedStock",
                table: "ProductStocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ProductStocks",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ProductId", "ReservedStock" },
                values: new object[] { "66c01f3e7b23d12a98f12345", 0 });

            migrationBuilder.UpdateData(
                table: "ProductStocks",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ProductId", "ReservedStock" },
                values: new object[] { "66c01f3e7b23d12a98f12346", 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedStock",
                table: "ProductStocks");

            migrationBuilder.UpdateData(
                table: "ProductStocks",
                keyColumn: "Id",
                keyValue: 1,
                column: "ProductId",
                value: "prod-1");

            migrationBuilder.UpdateData(
                table: "ProductStocks",
                keyColumn: "Id",
                keyValue: 2,
                column: "ProductId",
                value: "prod-2");
        }
    }
}
