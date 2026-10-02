using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class ClassListItemViewModel
{
    public int Id { get; set; }
    public string ClassName { get; set; } = null!;
    public string AcademicYearCode { get; set; } = null!;
    public string GradeLevelName { get; set; } = null!;
    public string GradeLevelCode { get; set; } = null!;
    public string? HomeroomTeacherName { get; set; }
    public string? HomeroomTeacherCode { get; set; }
    public int StudentCount { get; set; }
}

public class ClassListPageViewModel
{
    public int? SelectedAcademicYearId { get; set; }
    public int? SelectedGradeLevelId { get; set; }
    public IEnumerable<SelectListItem> AcademicYears { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> GradeLevels { get; set; } = Enumerable.Empty<SelectListItem>();
    public IReadOnlyList<ClassListItemViewModel> Classes { get; set; } = Array.Empty<ClassListItemViewModel>();
}
