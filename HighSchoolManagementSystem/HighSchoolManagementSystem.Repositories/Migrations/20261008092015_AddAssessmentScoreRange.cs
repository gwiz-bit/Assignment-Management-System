using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentScoreRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('assessment_types', 'max_score') IS NULL ALTER TABLE [assessment_types] ADD [max_score] decimal(5,2) NOT NULL DEFAULT (10);");
            migrationBuilder.Sql("IF COL_LENGTH('assessment_types', 'min_score') IS NULL ALTER TABLE [assessment_types] ADD [min_score] decimal(5,2) NOT NULL DEFAULT (0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('assessment_types', 'max_score') IS NOT NULL ALTER TABLE [assessment_types] DROP COLUMN [max_score];");
            migrationBuilder.Sql("IF COL_LENGTH('assessment_types', 'min_score') IS NOT NULL ALTER TABLE [assessment_types] DROP COLUMN [min_score];");
        }
    }
}
