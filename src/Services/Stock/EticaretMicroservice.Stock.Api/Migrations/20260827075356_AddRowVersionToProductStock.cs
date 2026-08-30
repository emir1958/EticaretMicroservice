using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Stock.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductStocks",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductStocks");
        }
    }
}
