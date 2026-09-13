using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class ColumnToShowConfigsInPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowInSettings",
                table: "Configurations",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowInSettings",
                table: "Configurations");
        }
    }
}
