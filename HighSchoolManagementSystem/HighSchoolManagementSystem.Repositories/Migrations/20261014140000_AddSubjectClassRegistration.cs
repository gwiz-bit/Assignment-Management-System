using HighSchoolManagementSystem.Repositories.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HighSchoolManagementSystem.Repositories.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014140000_AddSubjectClassRegistration")]
public partial class AddSubjectClassRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "subject_class_registrations",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                student_id = table.Column<int>(type: "int", nullable: false),
                teaching_assignment_id = table.Column<int>(type: "int", nullable: false),
                status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                registered_at = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_subject_class_registrations", x => x.id);
                table.ForeignKey("FK_subject_class_registrations_students_student_id", x => x.student_id, "students", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_subject_class_registrations_teaching_assignments_teaching_assignment_id", x => x.teaching_assignment_id, "teaching_assignments", "id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_subject_class_registrations_student_id_teaching_assignment_id", "subject_class_registrations", new[] { "student_id", "teaching_assignment_id" }, unique: true);
        migrationBuilder.CreateIndex("IX_subject_class_registrations_teaching_assignment_id", "subject_class_registrations", "teaching_assignment_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("subject_class_registrations");
}
