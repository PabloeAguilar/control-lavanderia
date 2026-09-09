using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class PrinterConfigurationsAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Configuration",
                columns: ["Key", "Value"],
                values: new object[,] {
                    { "PrinterIp", "" },
                    { "BusinessTittle", "Lavandería Ejemplo" },
                    { "BusinessRegimen", "Regimen fiscal" },
                    { "BusinessAddress", "Calle Falsa 123, Colonia Ejemplo, Ciudad, Estado, CP 12345" },
                    { "BusinessPhone", "(123) 456-7890" },
                    { "BusinessDays", "Lunes a Viernes: 9:00 AM - 6:00 PM\nSábado: 10:00 AM - 4:00 PM\nDomingo: Cerrado" },
                    { "BusinessMessage", "Gracias por su preferencia!" }
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Configuration",
                keyColumn: "Key",
                keyValues: ["PrinterIp", "BusinessTittle", "BusinessRegimen", "BusinessAddress", "BusinessPhone", "BusinessDays", "BusinessMessage"]);
        }
    }
}
