using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinationControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vaccines");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "VaccinationRecords");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "People");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vaccines",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "VaccinationRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "People",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
