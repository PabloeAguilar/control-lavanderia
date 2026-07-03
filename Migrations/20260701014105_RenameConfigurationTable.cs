using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class RenameConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Configuration",
                table: "Configuration");

            migrationBuilder.RenameTable(
                name: "Configuration",
                newName: "Configurations");

            migrationBuilder.RenameIndex(
                name: "IX_Configuration_Key",
                table: "Configurations",
                newName: "IX_Configurations_Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Configurations",
                table: "Configurations");

            migrationBuilder.RenameTable(
                name: "Configurations",
                newName: "Configuration");

            migrationBuilder.RenameIndex(
                name: "IX_Configurations_Key",
                table: "Configuration",
                newName: "IX_Configuration_Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Configuration",
                table: "Configuration",
                column: "Id");
        }
    }
}
