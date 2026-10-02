using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff")]
public class StaffController : Controller
{
    private readonly IAcademicYearService _academicYearService;

    public StaffController(IAcademicYearService academicYearService)
    {
        _academicYearService = academicYearService;
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    public async Task<IActionResult> AcademicYears()
    {
        var result = await _academicYearService.GetAllAsync();
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(Array.Empty<AcademicYearListItemViewModel>());
        }

        var today = DateTime.Today;
        var items = result.Data!
            .Select(y =>
            {
                var (status, css) = ResolveStatus(y.StartDate, y.EndDate, today);
                return new AcademicYearListItemViewModel
                {
                    Id = y.Id,
                    YearCode = y.YearCode,
                    StartDate = y.StartDate,
                    EndDate = y.EndDate,
                    SemesterCount = y.Semesters.Count,
                    ClassCount = y.Classes.Count,
                    Status = status,
                    StatusCssClass = css
                };
            })
            .ToList();

        return View(items);
    }

    private static (string Status, string CssClass) ResolveStatus(DateTime start, DateTime end, DateTime today)
    {
        if (today < start.Date)
            return ("Sắp diễn ra", "badge-status-info");
        if (today > end.Date)
            return ("Đã kết thúc", "badge-status-danger");
        return ("Đang diễn ra", "badge-status-success");
    }
}
