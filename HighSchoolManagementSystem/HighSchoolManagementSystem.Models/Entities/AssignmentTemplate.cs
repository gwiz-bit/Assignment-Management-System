using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighSchoolManagementSystem.Models.Entities;

[Table("assignment_templates")]
public class AssignmentTemplate
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(120), Column("title")]
    public string Title { get; set; } = null!;

    [MaxLength(4000), Column("description")]
    public string? Description { get; set; }

    [Column("subject_id")]
    public int SubjectId { get; set; }

    [Column("created_by_user_id")]
    public int CreatedByUserId { get; set; }

    [Column("is_published")]
    public bool IsPublished { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SubjectId))]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual User CreatedBy { get; set; } = null!;
}
