using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinationControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignDocumentLengthAndDoseTypeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VaccinationRecords_PersonId_VaccineId_DoseNumber",
                table: "VaccinationRecords");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_PersonId_VaccineId_VaccinationType_DoseNumber",
                table: "VaccinationRecords",
                columns: new[] { "PersonId", "VaccineId", "VaccinationType", "DoseNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VaccinationRecords_PersonId_VaccineId_VaccinationType_DoseNumber",
                table: "VaccinationRecords");

            migrationBuilder.CreateIndex(
                name: "IX_VaccinationRecords_PersonId_VaccineId_DoseNumber",
                table: "VaccinationRecords",
                columns: new[] { "PersonId", "VaccineId", "DoseNumber" },
                unique: true);
        }
    }
}
