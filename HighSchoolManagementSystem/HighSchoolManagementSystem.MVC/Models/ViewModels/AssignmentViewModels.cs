using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class AssignmentTemplateFormViewModel
{
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [StringLength(4000)] public string? Description { get; set; }
    [Range(1, int.MaxValue)] public int SubjectId { get; set; }
}

public class AssignmentSubmissionFormViewModel
{
    public int AssignmentId { get; set; }
    [Required, StringLength(8000, MinimumLength = 1)] public string Answer { get; set; } = "";
}

public class AssignmentGradeViewModel
{
    public int SubmissionId { get; set; }
    [Range(0, 100)] public decimal Score { get; set; }
    [StringLength(2000)] public string? TeacherFeedback { get; set; }
}

public class PracticeRequestViewModel
{
    [Range(1, int.MaxValue)] public int SubjectId { get; set; }
    [Required, StringLength(300)] public string Topic { get; set; } = "";
    public string? GeneratedPractice { get; set; }
}
