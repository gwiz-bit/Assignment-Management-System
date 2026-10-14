using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Student;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Implementations;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Admin")]
public class StudentManagementController(IStudentService students) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int? classId, bool unassignedOnly = false)
    {
        var result = await students.GetStudentsAsync(search, status, classId, unassignedOnly);
        var classes = await students.GetCurrentClassesAsync();
        var year = await students.GetCurrentYearAsync();
        if (!result.Success || !classes.Success || !year.Success)
            ViewData["ErrorMessage"] = result.Message ?? classes.Message ?? year.Message;
        return View(new StudentListViewModel
        {
            Search = search, Status = status, ClassId = classId, UnassignedOnly = unassignedOnly,
            CurrentYear = year.Data?.YearCode, Students = result.Data?.Select(Map).ToList() ?? new(),
            Classes = ClassOptions(classes.Data)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        return View(Map(result.Data));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateStudentViewModel { AdmissionDate = DateTime.UtcNow.Date });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await students.CreateStudentAsync(model);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo học sinh.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        ViewData["StudentId"] = id;
        return View((UpdateStudentDto)Map(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateStudentDto model)
    {
        ViewData["StudentId"] = id;
        if (!ModelState.IsValid) return View(model);
        var result = await students.UpdateStudentAsync(id, model);
        if (result.ErrorCode == StudentService.NotFound) return NotFound();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật học sinh.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public Task<IActionResult> AssignClass(int id) => AssignmentPage(id, 0);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignClass(int id, AssignStudentClassDto model)
    {
        if (ModelState.IsValid)
        {
            var result = await students.AssignClassAsync(id, model.ClassId);
            if (result.ErrorCode == StudentService.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể phân lớp.");
        }
        return await AssignmentPage(id, model.ClassId);
    }

    private async Task<IActionResult> AssignmentPage(int id, int selectedClass)
    {
        var result = await students.GetStudentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        var classes = await students.GetCurrentClassesAsync();
        var year = await students.GetCurrentYearAsync();
        if (!classes.Success || !year.Success) ModelState.AddModelError(string.Empty, classes.Message ?? year.Message ?? "Không thể tải lớp học.");
        return View("AssignClass", new AssignStudentClassViewModel
        {
            Student = Map(result.Data), ClassId = selectedClass, CurrentYear = year.Data?.YearCode,
            Classes = ClassOptions(classes.Data)
        });
    }

    private IActionResult ReadFailure(string? code, string? message)
    {
        if (code == StudentService.NotFound) return NotFound();
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private static List<SelectListItem> ClassOptions(List<Class>? classes) => classes?.Select(c =>
        new SelectListItem($"{c.ClassName} — {c.GradeLevel.LevelName} — {c.AcademicYear.YearCode}", c.Id.ToString())).ToList() ?? new();

    private static StudentDetailsViewModel Map(Student student)
    {
        var memberships = student.StudentClasses.Where(sc => sc.Class.AcademicYear.IsCurrent).ToList();
        var current = memberships.FirstOrDefault(sc => sc.Status == "Enrolled" && sc.LeftAt == null);
        return new StudentDetailsViewModel
        {
            Id = student.Id, StudentCode = student.StudentCode, FullName = student.User.FullName,
            Email = student.User.Email, PhoneNumber = student.User.PhoneNumber, Gender = student.User.Gender,
            DateOfBirth = student.User.DateOfBirth, AdmissionDate = student.AdmissionDate, Address = student.Address,
            Status = student.Status, Username = student.User.Username, IsAccountActive = student.User.IsActive,
            ParentId = student.ParentId, ParentName = student.Parent?.User.FullName, ParentPhone = student.Parent?.User.PhoneNumber,
            ParentEmail = student.Parent?.User.Email, HasAssignment = memberships.Count > 0,
            CurrentClassId = current?.ClassId, CurrentClassName = current?.Class.ClassName,
            CurrentGrade = current?.Class.GradeLevel.LevelName, CurrentYear = current?.Class.AcademicYear.YearCode
        };
    }
}
