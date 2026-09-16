using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurationForRFC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                "Configurations",
                columns: ["Key", "Value"],
                values: new object[,]
                {
                    {"BusinessRFC", "XXXXXXXXXXXXXX"}
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configurations",
                keyColumn: "Key",
                keyValues: ["BusinessRFC"]
            );
        }
    }
}
