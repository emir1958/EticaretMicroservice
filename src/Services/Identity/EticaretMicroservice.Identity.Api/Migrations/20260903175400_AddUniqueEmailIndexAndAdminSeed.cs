using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Identity.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEmailIndexAndAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "User",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role", "Username" },
                values: new object[] { "b7a2d480-1a22-4826-b841-3965d1d60001", "admin@eticaret.com", "$2a$11$Vgn9HlfqINviXCCEo3SV1Occ0wMFJrLTdl8Ak/ESP8a2Pt/wjCuTO", "Admin", "SystemAdmin" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "b7a2d480-1a22-4826-b841-3965d1d60001");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "User");
        }
    }
}
