using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EticaretMicroservice.Identity.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAdminHasDataSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "b7a2d480-1a22-4826-b841-3965d1d60001");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role", "Username" },
                values: new object[] { "b7a2d480-1a22-4826-b841-3965d1d60001", "admin@eticaret.com", "$2a$11$Vgn9HlfqINviXCCEo3SV1Occ0wMFJrLTdl8Ak/ESP8a2Pt/wjCuTO", "Admin", "SystemAdmin" });
        }
    }
}
