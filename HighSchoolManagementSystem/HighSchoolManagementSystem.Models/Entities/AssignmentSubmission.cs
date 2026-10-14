using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("assignment_submissions")]
public class AssignmentSubmission
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("assignment_template_id")]
    public int AssignmentTemplateId { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Required, MaxLength(8000), Column("answer")]
    public string Answer { get; set; } = null!;

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Column("score")]
    public decimal? Score { get; set; }

    [MaxLength(2000), Column("teacher_feedback")]
    public string? TeacherFeedback { get; set; }

    [Column("evaluated_by_user_id")]
    public int? EvaluatedByUserId { get; set; }

    [Column("evaluated_at")]
    public DateTime? EvaluatedAt { get; set; }

    [ForeignKey(nameof(AssignmentTemplateId))]
    public virtual AssignmentTemplate AssignmentTemplate { get; set; } = null!;

    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey(nameof(EvaluatedByUserId))]
    public virtual User? EvaluatedBy { get; set; }
}
