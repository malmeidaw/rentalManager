using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorbikeConsumer.Migrations
{
    /// <inheritdoc />
    public partial class Start : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rental_manager");

            migrationBuilder.CreateTable(
                name: "delivery_man",
                schema: "rental_manager",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    cnpj = table.Column<string>(type: "text", nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    drivers_license = table.Column<int>(type: "integer", nullable: false),
                    drivers_license_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_man", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "motorbike",
                schema: "rental_manager",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorbike", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_motorbike_plate",
                schema: "rental_manager",
                table: "motorbike",
                column: "plate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_man",
                schema: "rental_manager");

            migrationBuilder.DropTable(
                name: "motorbike",
                schema: "rental_manager");
        }
    }
}
