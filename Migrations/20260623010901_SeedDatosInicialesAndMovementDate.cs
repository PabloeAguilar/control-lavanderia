using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class SeedDatosInicialesAndMovementDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedDate",
                table: "OrderMovements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RegisterDate",
                table: "OrderMovements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.InsertData(
                table: "Configuration",
                columns: ["Key", "Value"],
                values: new object[,]
                {
                    { "PrecioLavado", "20" },
                    { "PrecioPlanchado", "15" }
                });

            migrationBuilder.InsertData(
                table: "Suggestions",
                columns: ["Name", "Key", "Price"],
                values: new object[,]
                {
                    { "Cobertor", "cobertor", "60" },
                    { "Edredon", "edredon", "80" },
                    { "Almohada", "almohada", "25" },
                    { "Par de tenis", "par-tenis", "50" },
                    { "Pieza de ropa", "pieza-ropa", "7" }
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: ["Name", "Position", "Description", "MaxCapacity"],
                values: new object[,]
                {
                    { "Lavadoras", "1", "Área de lavado principal", 4 },
                    { "Secadoras", "2", "Área de secado principal", 2 },
                    { "Tendederos", "3", "Área de planchado y doblado", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Name",
                keyValues: ["Lavadoras", "Secadoras", "Tendederos"]);

            migrationBuilder.DeleteData(
                table: "Suggestions",
                keyColumn: "Key",
                keyValues: ["cobertor", "edredon", "almohada", "par-tenis", "pieza-ropa"]);

            migrationBuilder.DeleteData(
                table: "Configuration",
                keyColumn: "Key",
                keyValues: ["PrecioLavado", "PrecioPlanchado"]);

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "OrderMovements");

            migrationBuilder.DropColumn(
                name: "RegisterDate",
                table: "OrderMovements");

        }
    }
}
