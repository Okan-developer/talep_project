using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TalepYonetim.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "department",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "department",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("019aa000-0000-7000-8000-000000000001"), "İnsan Kaynakları" },
                    { new Guid("019aa000-0000-7000-8000-000000000002"), "Muhasebe" },
                    { new Guid("019aa000-0000-7000-8000-000000000003"), "Finans" },
                    { new Guid("019aa000-0000-7000-8000-000000000004"), "Operasyon" },
                    { new Guid("019aa000-0000-7000-8000-000000000005"), "BT Yazılım" },
                    { new Guid("019aa000-0000-7000-8000-000000000006"), "BT Sistem" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_department_name",
                schema: "identity",
                table: "department",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "department",
                schema: "identity");
        }
    }
}
