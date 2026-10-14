using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.Repositories.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Expert,Admin")]
public class ExpertController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TemplateCount = await db.AssignmentTemplates.CountAsync();
        ViewBag.PublishedCount = await db.AssignmentTemplates.CountAsync(t => t.IsPublished);
        ViewBag.SubjectClassCount = await db.TeachingAssignments.CountAsync(t => t.Semester.IsActive && t.Semester.AcademicYear.IsCurrent);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SubjectClasses()
    {
        var items = await db.TeachingAssignments.AsNoTracking()
            .Include(t => t.Subject).Include(t => t.Teacher).ThenInclude(t => t.User)
            .Include(t => t.Class).ThenInclude(c => c.AcademicYear)
            .Include(t => t.Semester).ThenInclude(s => s.AcademicYear)
            .OrderBy(t => t.Subject.SubjectName).ThenBy(t => t.Class.ClassName).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSubjectClass()
    {
        var model = new CreateSubjectClassViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubjectClass(CreateSubjectClassViewModel model)
    {
        var valid = await db.Teachers.AnyAsync(t => t.Id == model.TeacherId && t.Status == "Active")
            && await db.Subjects.AnyAsync(s => s.Id == model.SubjectId && s.IsActive)
            && await db.Classes.AnyAsync(c => c.Id == model.ClassId && c.AcademicYear.IsCurrent)
            && await db.Semesters.AnyAsync(s => s.Id == model.SemesterId && s.IsActive && s.AcademicYear.IsCurrent);
        if (!valid) ModelState.AddModelError(string.Empty, "Chọn giáo viên, môn học, lớp và học kỳ đang hoạt động.");
        if (await db.TeachingAssignments.AnyAsync(t => t.TeacherId == model.TeacherId && t.SubjectId == model.SubjectId && t.ClassId == model.ClassId && t.SemesterId == model.SemesterId))
            ModelState.AddModelError(string.Empty, "Lớp môn này đã được phân công.");
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }
        db.TeachingAssignments.Add(new TeachingAssignment
        {
            TeacherId = model.TeacherId, SubjectId = model.SubjectId, ClassId = model.ClassId,
            SemesterId = model.SemesterId, Status = "Assigned"
        });
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã tạo subject class.";
        return RedirectToAction(nameof(SubjectClasses));
    }

    private async Task PopulateOptionsAsync(CreateSubjectClassViewModel model)
    {
        model.Teachers = await db.Teachers.Where(t => t.Status == "Active").OrderBy(t => t.User.FullName)
            .Select(t => new SelectListItem(t.User.FullName, t.Id.ToString())).ToListAsync();
        model.Subjects = await db.Subjects.Where(s => s.IsActive).OrderBy(s => s.SubjectName)
            .Select(s => new SelectListItem(s.SubjectName, s.Id.ToString())).ToListAsync();
        model.Classes = await db.Classes.Where(c => c.AcademicYear.IsCurrent).OrderBy(c => c.ClassName)
            .Select(c => new SelectListItem(c.ClassName + " · " + c.GradeLevel.LevelName, c.Id.ToString())).ToListAsync();
        model.Semesters = await db.Semesters.Where(s => s.IsActive && s.AcademicYear.IsCurrent).OrderBy(s => s.SemesterName)
            .Select(s => new SelectListItem(s.SemesterName + " · " + s.AcademicYear.YearCode, s.Id.ToString())).ToListAsync();
    }
}
