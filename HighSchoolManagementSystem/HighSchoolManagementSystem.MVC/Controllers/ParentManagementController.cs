using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Parent;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Implementations;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Staff,Admin")]
public class ParentManagementController(IParentService parents) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool? isActive)
    {
        var result = await parents.GetParentsAsync(search, isActive);
        if (!result.Success) ViewData["ErrorMessage"] = result.Message;
        return View(new ParentListViewModel
        {
            Search = search, IsActive = isActive,
            Parents = result.Data?.Select(Map).ToList() ?? new()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await parents.GetParentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        return View(Map(result.Data));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateParentViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateParentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await parents.CreateParentAsync(model);
        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể tạo phụ huynh.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Data.ParentId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await parents.GetParentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        ViewData["ParentId"] = id;
        return View((UpdateParentDto)Map(result.Data));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateParentDto model)
    {
        ViewData["ParentId"] = id;
        if (!ModelState.IsValid) return View(model);
        var result = await parents.UpdateParentAsync(id, model);
        if (result.ErrorCode == ParentService.NotFound) return NotFound();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể cập nhật phụ huynh.");
            return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public Task<IActionResult> LinkStudents(int id, string? search) => LinkPage(id, search, new());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkStudents(int id, List<int> selectedStudentIds, string? search)
    {
        if (ModelState.IsValid)
        {
            var result = await parents.LinkStudentsAsync(id, selectedStudentIds);
            if (result.ErrorCode == ParentService.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Message ?? "Không thể liên kết học sinh.");
        }
        return await LinkPage(id, search, selectedStudentIds);
    }

    private async Task<IActionResult> LinkPage(int id, string? search, List<int> selected)
    {
        var result = await parents.GetParentAsync(id);
        if (!result.Success || result.Data == null) return ReadFailure(result.ErrorCode, result.Message);
        var model = new ParentLinkViewModel { Parent = Map(result.Data), Search = search, SelectedStudentIds = selected };
        if (model.Parent.CanLink)
        {
            var students = await parents.GetAvailableStudentsAsync(search);
            if (!students.Success) ModelState.AddModelError(string.Empty, students.Message ?? "Không thể tải học sinh.");
            model.AvailableStudents = students.Data?.Select(MapStudent).ToList() ?? new();
        }
        return View("LinkStudents", model);
    }

    private IActionResult ReadFailure(string? code, string? message)
    {
        if (code == ParentService.NotFound) return NotFound();
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private static ParentDetailsViewModel Map(Parent parent) => new()
    {
        Id = parent.ParentId, Username = parent.User.Username, FullName = parent.User.FullName,
        Email = parent.User.Email, PhoneNumber = parent.User.PhoneNumber, Gender = parent.User.Gender,
        DateOfBirth = parent.User.DateOfBirth, IsActive = parent.User.IsActive, RoleName = parent.User.Role.RoleName,
        Occupation = parent.Occupation, EmergencyContact = parent.EmergencyContact,
        Students = parent.Students.OrderBy(s => s.StudentCode).Select(MapStudent).ToList()
    };

    private static ParentStudentViewModel MapStudent(Student student) => new()
    {
        Id = student.Id, StudentCode = student.StudentCode, FullName = student.User?.FullName ?? string.Empty, Status = student.Status
    };
}
