using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicYearIsCurrent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_current",
                table: "academic_years",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_academic_years_is_current",
                table: "academic_years",
                column: "is_current",
                unique: true,
                filter: "[is_current] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_academic_years_is_current",
                table: "academic_years");

            migrationBuilder.DropColumn(
                name: "is_current",
                table: "academic_years");
        }
    }
}
