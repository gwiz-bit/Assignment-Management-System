using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.ActiveUsers = await db.Users.CountAsync(u => u.IsActive);
        ViewBag.SubjectClasses = await db.TeachingAssignments.CountAsync(t => t.Status == "Assigned" && t.Semester.IsActive && t.Semester.AcademicYear.IsCurrent);
        ViewBag.AssignmentTemplates = await db.AssignmentTemplates.CountAsync(t => t.IsPublished);
        ViewBag.PendingSubmissions = await db.AssignmentSubmissions.CountAsync(s => s.EvaluatedAt == null);
        ViewBag.PendingRegistrations = await db.SubjectClassRegistrations.CountAsync(r => r.Status == "PendingApproval");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ClassRegistrations()
    {
        var requests = await db.SubjectClassRegistrations.AsNoTracking()
            .Where(r => r.Status == "PendingApproval")
            .Include(r => r.Student).ThenInclude(s => s.User)
            .Include(r => r.TeachingAssignment).ThenInclude(t => t.Subject)
            .Include(r => r.TeachingAssignment).ThenInclude(t => t.Class)
            .Include(r => r.TeachingAssignment).ThenInclude(t => t.Semester)
            .OrderBy(r => r.RegisteredAt).ToListAsync();
        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewRegistration(int id, bool approve)
    {
        var registration = await db.SubjectClassRegistrations.SingleOrDefaultAsync(r => r.Id == id && r.Status == "PendingApproval");
        if (registration == null) return NotFound();
        registration.Status = approve ? "Enrolled" : "Rejected";
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = approve ? "Đã duyệt đăng ký lớp môn." : "Đã từ chối đăng ký lớp môn.";
        return RedirectToAction(nameof(ClassRegistrations));
    }
}
