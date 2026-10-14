using HighSchoolManagementSystem.Repositories.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014120000_AddAssignmentWorkflows")]
public partial class AddAssignmentWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "assignment_templates",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                subject_id = table.Column<int>(type: "int", nullable: false),
                created_by_user_id = table.Column<int>(type: "int", nullable: false),
                is_published = table.Column<bool>(type: "bit", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_assignment_templates", x => x.id);
                table.ForeignKey("FK_assignment_templates_subjects_subject_id", x => x.subject_id, "subjects", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_assignment_templates_users_created_by_user_id", x => x.created_by_user_id, "users", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "assignment_submissions",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                assignment_template_id = table.Column<int>(type: "int", nullable: false),
                student_id = table.Column<int>(type: "int", nullable: false),
                answer = table.Column<string>(type: "nvarchar(8000)", maxLength: 8000, nullable: false),
                submitted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                teacher_feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                evaluated_by_user_id = table.Column<int>(type: "int", nullable: true),
                evaluated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_assignment_submissions", x => x.id);
                table.ForeignKey("FK_assignment_submissions_assignment_templates_assignment_template_id", x => x.assignment_template_id, "assignment_templates", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_assignment_submissions_students_student_id", x => x.student_id, "students", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_assignment_submissions_users_evaluated_by_user_id", x => x.evaluated_by_user_id, "users", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_assignment_templates_created_by_user_id", "assignment_templates", "created_by_user_id");
        migrationBuilder.CreateIndex("IX_assignment_templates_subject_id", "assignment_templates", "subject_id");
        migrationBuilder.CreateIndex("IX_assignment_submissions_assignment_template_id_student_id", "assignment_submissions", new[] { "assignment_template_id", "student_id" }, unique: true);
        migrationBuilder.CreateIndex("IX_assignment_submissions_evaluated_by_user_id", "assignment_submissions", "evaluated_by_user_id");
        migrationBuilder.CreateIndex("IX_assignment_submissions_student_id", "assignment_submissions", "student_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("assignment_submissions");
        migrationBuilder.DropTable("assignment_templates");
    }
}
