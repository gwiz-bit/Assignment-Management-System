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

    [HttpGet]
    public IActionResult CreateAcademicYear()
    {
        var currentYear = DateTime.Today.Year;
        // Nếu đang từ tháng 1-5: năm học đang chạy là (currentYear-1)-currentYear
        // Từ tháng 6 trở đi: gợi ý năm học mới currentYear-(currentYear+1)
        var startYear = DateTime.Today.Month >= 6 ? currentYear : currentYear - 1;

        var model = new CreateAcademicYearViewModel
        {
            YearCode = $"{startYear + 1}-{startYear + 2}",
            StartDate = new DateTime(startYear + 1, 9, 5),
            EndDate = new DateTime(startYear + 2, 5, 31)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAcademicYear(CreateAcademicYearViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.StartDate.Date >= model.EndDate.Date)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var result = await _academicYearService.CreateAsync(model.YearCode, model.StartDate, model.EndDate);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo năm học.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã tạo năm học \"{result.Data!.YearCode}\" thành công.";
        return RedirectToAction(nameof(AcademicYears));
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
