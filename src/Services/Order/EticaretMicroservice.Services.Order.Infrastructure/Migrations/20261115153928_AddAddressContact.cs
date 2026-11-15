using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Services.Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                schema: "ordering",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "ordering",
                table: "Orders",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactName",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "ordering",
                table: "Orders");
        }
    }
}
