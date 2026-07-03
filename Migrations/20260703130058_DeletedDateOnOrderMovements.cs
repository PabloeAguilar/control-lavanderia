using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace controllavanderia.Migrations
{
    /// <inheritdoc />
    public partial class DeletedDateOnOrderMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedDate",
                table: "OrderMovements",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "OrderMovements");
        }
    }
}
