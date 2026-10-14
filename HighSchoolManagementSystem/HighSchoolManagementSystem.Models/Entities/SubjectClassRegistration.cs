using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("subject_class_registrations")]
public class SubjectClassRegistration
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("teaching_assignment_id")]
    public int TeachingAssignmentId { get; set; }

    [Required, MaxLength(30), Column("status")]
    public string Status { get; set; } = "PendingApproval";

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(TeachingAssignmentId))]
    public virtual TeachingAssignment TeachingAssignment { get; set; } = null!;
}
