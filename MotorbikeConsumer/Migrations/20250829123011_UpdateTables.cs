using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorbikeConsumer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_delivery_man_cnpj",
                schema: "rental_manager",
                table: "delivery_man",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_man_drivers_license",
                schema: "rental_manager",
                table: "delivery_man",
                column: "drivers_license",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_delivery_man_cnpj",
                schema: "rental_manager",
                table: "delivery_man");

            migrationBuilder.DropIndex(
                name: "IX_delivery_man_drivers_license",
                schema: "rental_manager",
                table: "delivery_man");
        }
    }
}
