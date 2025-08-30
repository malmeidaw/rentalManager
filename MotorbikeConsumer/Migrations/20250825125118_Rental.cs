using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorbikeConsumer.Migrations
{
    /// <inheritdoc />
    public partial class Rental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rental",
                schema: "rental_manager",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    delivery_man_id = table.Column<string>(type: "text", nullable: false),
                    motorbike_id = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rental_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rental", x => x.id);
                    table.ForeignKey(
                        name: "FK_rental_delivery_man_delivery_man_id",
                        column: x => x.delivery_man_id,
                        principalSchema: "rental_manager",
                        principalTable: "delivery_man",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rental_motorbike_motorbike_id",
                        column: x => x.motorbike_id,
                        principalSchema: "rental_manager",
                        principalTable: "motorbike",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rental_delivery_man_id",
                schema: "rental_manager",
                table: "rental",
                column: "delivery_man_id");

            migrationBuilder.CreateIndex(
                name: "IX_rental_motorbike_id",
                schema: "rental_manager",
                table: "rental",
                column: "motorbike_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rental",
                schema: "rental_manager");
        }
    }
}
