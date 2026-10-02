namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class SemesterListItemViewModel
{
    public int Id { get; set; }
    public string SemesterCode { get; set; } = null!;
    public string SemesterName { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!;
    public string StatusCssClass { get; set; } = null!;
}

public class SemesterListPageViewModel
{
    public int AcademicYearId { get; set; }
    public string AcademicYearCode { get; set; } = null!;
    public DateTime AcademicYearStartDate { get; set; }
    public DateTime AcademicYearEndDate { get; set; }
    public bool IsCurrentAcademicYear { get; set; }
    public IReadOnlyList<SemesterListItemViewModel> Semesters { get; set; } = Array.Empty<SemesterListItemViewModel>();
}
