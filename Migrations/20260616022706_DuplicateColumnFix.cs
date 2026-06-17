using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class DuplicateColumnFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill IdCliente from the old shadow FK column when possible.
            migrationBuilder.Sql(@"
                UPDATE ""Orders""
                SET ""IdCliente"" = COALESCE(""ClientId"", ""IdCliente"")
                WHERE ""ClientId"" IS NOT NULL;");

            // Remove rows that would violate the new FK (invalid client references).
            migrationBuilder.Sql(@"
                DELETE FROM ""Orders"" o
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""Clients"" c
                    WHERE c.""Id"" = o.""IdCliente""
                );");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Clients_ClientId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ClientId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IdCliente",
                table: "Orders",
                column: "IdCliente");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Clients_IdCliente",
                table: "Orders",
                column: "IdCliente",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Clients_IdCliente",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_IdCliente",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientId",
                table: "Orders",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Clients_ClientId",
                table: "Orders",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }
    }
}
