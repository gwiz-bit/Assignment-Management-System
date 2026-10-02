using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff")]
public class StaffController : Controller
{
    private readonly IAcademicYearService _academicYearService;
    private readonly ISemesterService _semesterService;
    private readonly IClassService _classService;
    private readonly IGradeLevelService _gradeLevelService;

    public StaffController(
        IAcademicYearService academicYearService,
        ISemesterService semesterService,
        IClassService classService,
        IGradeLevelService gradeLevelService)
    {
        _academicYearService = academicYearService;
        _semesterService = semesterService;
        _classService = classService;
        _gradeLevelService = gradeLevelService;
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
                    IsCurrent = y.IsCurrent,
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

    [HttpGet]
    public async Task<IActionResult> EditAcademicYear(int id)
    {
        var result = await _academicYearService.GetByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy năm học.";
            return RedirectToAction(nameof(AcademicYears));
        }

        var model = new EditAcademicYearViewModel
        {
            Id = result.Data.Id,
            YearCode = result.Data.YearCode,
            StartDate = result.Data.StartDate,
            EndDate = result.Data.EndDate
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAcademicYear(EditAcademicYearViewModel model)
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

        var result = await _academicYearService.UpdateAsync(model.Id, model.YearCode, model.StartDate, model.EndDate);
        if (!result.Success)
        {
            if (result.ErrorCode == ErrorCodes.ResourceNotFound)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(AcademicYears));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật năm học.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật năm học \"{result.Data!.YearCode}\" thành công.";
        return RedirectToAction(nameof(AcademicYears));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrentAcademicYear(int id)
    {
        var result = await _academicYearService.SetCurrentAsync(id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể đặt năm học hiện hành.";
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(AcademicYears));
    }

    // =========================================================================
    // HỌC KỲ
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Semesters(int academicYearId)
    {
        var yearResult = await _academicYearService.GetByIdAsync(academicYearId);
        if (!yearResult.Success || yearResult.Data == null)
        {
            TempData["ErrorMessage"] = yearResult.Message ?? "Không tìm thấy năm học.";
            return RedirectToAction(nameof(AcademicYears));
        }

        var semesterResult = await _semesterService.GetByAcademicYearIdAsync(academicYearId);
        if (!semesterResult.Success)
        {
            TempData["ErrorMessage"] = semesterResult.Message;
            return RedirectToAction(nameof(AcademicYears));
        }

        var today = DateTime.Today;
        var year = yearResult.Data;
        var page = new SemesterListPageViewModel
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.YearCode,
            AcademicYearStartDate = year.StartDate,
            AcademicYearEndDate = year.EndDate,
            IsCurrentAcademicYear = year.IsCurrent,
            Semesters = semesterResult.Data!
                .Select(s =>
                {
                    var (periodStatus, periodCss) = ResolveStatus(s.StartDate, s.EndDate, today);
                    return new SemesterListItemViewModel
                    {
                        Id = s.Id,
                        SemesterCode = s.SemesterCode,
                        SemesterName = s.SemesterName,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        IsActive = s.IsActive,
                        PeriodStatus = periodStatus,
                        PeriodStatusCssClass = periodCss
                    };
                })
                .ToList()
        };

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateSemester(int id, int academicYearId)
    {
        var result = await _semesterService.ActivateAsync(id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể kích hoạt học kỳ.";
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Semesters), new { academicYearId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSemester(int id, int academicYearId)
    {
        var result = await _semesterService.CloseAsync(id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không thể đóng học kỳ.";
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Semesters), new { academicYearId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateSemester(int academicYearId)
    {
        var yearResult = await _academicYearService.GetByIdAsync(academicYearId);
        if (!yearResult.Success || yearResult.Data == null)
        {
            TempData["ErrorMessage"] = yearResult.Message ?? "Không tìm thấy năm học.";
            return RedirectToAction(nameof(AcademicYears));
        }

        var year = yearResult.Data;
        var existing = await _semesterService.GetByAcademicYearIdAsync(academicYearId);
        var nextIndex = (existing.Data?.Count ?? 0) + 1;

        var yearParts = year.YearCode.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var yearSuffix = yearParts.Length == 2
                         && yearParts[0].Length >= 2
                         && yearParts[1].Length >= 2
            ? $"{yearParts[0][^2..]}{yearParts[1][^2..]}"
            : year.Id.ToString();

        // Gợi ý HK1: nửa đầu năm học, HK2: nửa sau
        var midPoint = year.StartDate.AddDays((year.EndDate - year.StartDate).TotalDays / 2);
        DateTime suggestStart;
        DateTime suggestEnd;
        if (nextIndex == 1)
        {
            suggestStart = year.StartDate;
            suggestEnd = midPoint;
        }
        else
        {
            suggestStart = midPoint.AddDays(1);
            suggestEnd = year.EndDate;
        }

        var model = new CreateSemesterViewModel
        {
            AcademicYearId = year.Id,
            AcademicYearCode = year.YearCode,
            AcademicYearStartDate = year.StartDate,
            AcademicYearEndDate = year.EndDate,
            SemesterCode = $"HK{nextIndex}-{yearSuffix}",
            SemesterName = $"Học kỳ {nextIndex}",
            StartDate = suggestStart.Date,
            EndDate = suggestEnd.Date
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSemester(CreateSemesterViewModel model)
    {
        await PopulateAcademicYearInfoAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.StartDate.Date >= model.EndDate.Date)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var result = await _semesterService.CreateAsync(
            model.AcademicYearId,
            model.SemesterCode,
            model.SemesterName,
            model.StartDate,
            model.EndDate);

        if (!result.Success)
        {
            if (result.ErrorCode == ErrorCodes.ResourceNotFound)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(AcademicYears));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo học kỳ.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã tạo học kỳ \"{result.Data!.SemesterName}\" thành công.";
        return RedirectToAction(nameof(Semesters), new { academicYearId = model.AcademicYearId });
    }

    [HttpGet]
    public async Task<IActionResult> EditSemester(int id)
    {
        var result = await _semesterService.GetByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Không tìm thấy học kỳ.";
            return RedirectToAction(nameof(AcademicYears));
        }

        var semester = result.Data;
        var year = semester.AcademicYear;

        var model = new EditSemesterViewModel
        {
            Id = semester.Id,
            AcademicYearId = semester.AcademicYearId,
            AcademicYearCode = year.YearCode,
            AcademicYearStartDate = year.StartDate,
            AcademicYearEndDate = year.EndDate,
            SemesterCode = semester.SemesterCode,
            SemesterName = semester.SemesterName,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSemester(EditSemesterViewModel model)
    {
        await PopulateAcademicYearInfoAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.StartDate.Date >= model.EndDate.Date)
        {
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            return View(model);
        }

        var result = await _semesterService.UpdateAsync(
            model.Id,
            model.SemesterCode,
            model.SemesterName,
            model.StartDate,
            model.EndDate);

        if (!result.Success)
        {
            if (result.ErrorCode is ErrorCodes.SemesterNotFound or ErrorCodes.ResourceNotFound)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(AcademicYears));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật học kỳ.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật học kỳ \"{result.Data!.SemesterName}\" thành công.";
        return RedirectToAction(nameof(Semesters), new { academicYearId = model.AcademicYearId });
    }

    // =========================================================================
    // LỚP HỌC
    // =========================================================================

    [HttpGet]
    public async Task<IActionResult> Classes(int? academicYearId, int? gradeLevelId)
    {
        var yearsResult = await _academicYearService.GetAllAsync();
        var gradesResult = await _gradeLevelService.GetAllAsync();

        if (!yearsResult.Success || !gradesResult.Success)
        {
            TempData["ErrorMessage"] = yearsResult.Message ?? gradesResult.Message ?? "Không thể tải bộ lọc lớp học.";
            return View(new ClassListPageViewModel());
        }

        // Lần đầu vào trang (không có query): mặc định năm học hiện hành.
        // Nếu user chọn "-- Tất cả năm học --" thì query có academicYearId rỗng → không gán mặc định.
        if (!Request.Query.ContainsKey("academicYearId") && !academicYearId.HasValue)
        {
            var currentYear = yearsResult.Data!.FirstOrDefault(y => y.IsCurrent);
            academicYearId = currentYear?.Id;
        }

        var classesResult = await _classService.GetByFiltersAsync(academicYearId, gradeLevelId);
        if (!classesResult.Success)
        {
            TempData["ErrorMessage"] = classesResult.Message;
        }

        var page = new ClassListPageViewModel
        {
            SelectedAcademicYearId = academicYearId,
            SelectedGradeLevelId = gradeLevelId,
            AcademicYears = yearsResult.Data!
                .Select(y => new SelectListItem
                {
                    Value = y.Id.ToString(),
                    Text = y.IsCurrent ? $"{y.YearCode} (Hiện hành)" : y.YearCode,
                    Selected = academicYearId == y.Id
                }),
            GradeLevels = gradesResult.Data!
                .Select(g => new SelectListItem
                {
                    Value = g.Id.ToString(),
                    Text = g.LevelName,
                    Selected = gradeLevelId == g.Id
                }),
            Classes = classesResult.Success && classesResult.Data != null
                ? classesResult.Data
                    .Select(c => new ClassListItemViewModel
                    {
                        Id = c.Id,
                        ClassName = c.ClassName,
                        AcademicYearCode = c.AcademicYear.YearCode,
                        GradeLevelName = c.GradeLevel.LevelName,
                        GradeLevelCode = c.GradeLevel.LevelCode,
                        HomeroomTeacherName = c.HomeroomTeacher?.User?.FullName,
                        HomeroomTeacherCode = c.HomeroomTeacher?.TeacherCode,
                        StudentCount = c.StudentClasses.Count(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
                    })
                    .ToList()
                : Array.Empty<ClassListItemViewModel>()
        };

        return View(page);
    }

    private async Task PopulateAcademicYearInfoAsync(CreateSemesterViewModel model)
    {
        var yearResult = await _academicYearService.GetByIdAsync(model.AcademicYearId);
        if (yearResult.Success && yearResult.Data != null)
        {
            model.AcademicYearCode = yearResult.Data.YearCode;
            model.AcademicYearStartDate = yearResult.Data.StartDate;
            model.AcademicYearEndDate = yearResult.Data.EndDate;
        }
    }

    private async Task PopulateAcademicYearInfoAsync(EditSemesterViewModel model)
    {
        var yearResult = await _academicYearService.GetByIdAsync(model.AcademicYearId);
        if (yearResult.Success && yearResult.Data != null)
        {
            model.AcademicYearCode = yearResult.Data.YearCode;
            model.AcademicYearStartDate = yearResult.Data.StartDate;
            model.AcademicYearEndDate = yearResult.Data.EndDate;
        }
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
