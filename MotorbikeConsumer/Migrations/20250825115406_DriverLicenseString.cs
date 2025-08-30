using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorbikeConsumer.Migrations
{
    /// <inheritdoc />
    public partial class DriverLicenseString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "drivers_license",
                schema: "rental_manager",
                table: "delivery_man",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "drivers_license",
                schema: "rental_manager",
                table: "delivery_man",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
