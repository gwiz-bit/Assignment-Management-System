using System.Security.Claims;
using HighSchoolManagementSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HighSchoolManagementSystem.MVC.Models.ViewModels.Score;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly IStudentRepository _studentRepository;
    private readonly AppDbContext _db;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IScoreService _scoreService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IStudentRepository studentRepository,
        AppDbContext db,
        IAcademicYearRepository academicYearRepository,
        IScoreService scoreService,
        ILogger<StudentController> logger)
    {
        _studentRepository = studentRepository;
        _db = db;
        _academicYearRepository = academicYearRepository;
        _scoreService = scoreService;
        _logger = logger;
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Classes()
    {
        var student = await ResolveCurrentStudentAsync();
        if (student == null) return Forbid();
        var classes = await _db.TeachingAssignments.AsNoTracking()
            .Where(t => t.Status == "Assigned" && t.Semester.IsActive && t.Semester.AcademicYear.IsCurrent && t.Subject.IsActive)
            .Include(t => t.Class).ThenInclude(c => c.GradeLevel)
            .Include(t => t.Class).ThenInclude(c => c.AcademicYear)
            .Include(t => t.Subject).Include(t => t.Teacher).ThenInclude(t => t.User)
            .Include(t => t.Semester)
            .OrderBy(t => t.Subject.SubjectName).ThenBy(t => t.Class.ClassName).ToListAsync();
        var assignmentIds = classes.Select(t => t.Id).ToArray();
        ViewBag.RegistrationStatus = await _db.SubjectClassRegistrations
            .Where(r => r.StudentId == student.Id && assignmentIds.Contains(r.TeachingAssignmentId))
            .ToDictionaryAsync(r => r.TeachingAssignmentId, r => r.Status);
        return View(classes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterClass(int classId)
    {
        var student = await ResolveCurrentStudentAsync();
        if (student == null) return Forbid();
        var targetClass = await _db.TeachingAssignments.SingleOrDefaultAsync(t => t.Id == classId && t.Status == "Assigned" && t.Semester.IsActive && t.Semester.AcademicYear.IsCurrent);
        if (targetClass == null) return NotFound();
        var registration = await _db.SubjectClassRegistrations.SingleOrDefaultAsync(sc => sc.StudentId == student.Id && sc.TeachingAssignmentId == classId);
        if (registration == null)
            _db.SubjectClassRegistrations.Add(new SubjectClassRegistration { StudentId = student.Id, TeachingAssignmentId = classId, Status = "PendingApproval", RegisteredAt = DateTime.UtcNow });
        else if (registration.Status == "Rejected")
            registration.Status = "PendingApproval";
        else
        {
            TempData["ErrorMessage"] = "Yêu cầu đăng ký lớp này đã được gửi.";
            return RedirectToAction(nameof(Classes));
        }
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã gửi đăng ký subject class để duyệt.";
        return RedirectToAction(nameof(Classes));
    }

    [HttpGet]
    public async Task<IActionResult> Scores(int? semesterId, int? subjectId)
    {
        var student = await ResolveCurrentStudentAsync();
        if (student == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hồ sơ học sinh của tài khoản này.";
            return RedirectToAction(nameof(Dashboard));
        }

        var allYears = await _academicYearRepository.GetAllAsync();
        var semestersList = new List<SemesterItemOption>();
        foreach (var yr in allYears)
        {
            foreach (var sem in yr.Semesters)
            {
                semestersList.Add(new SemesterItemOption
                {
                    Id = sem.Id,
                    SemesterName = sem.SemesterName,
                    AcademicYearCode = yr.YearCode,
                    IsActive = sem.IsActive
                });
            }
        }

        int targetSemesterId = semesterId ?? (semestersList.FirstOrDefault(s => s.IsActive)?.Id ?? (semestersList.FirstOrDefault()?.Id ?? 0));

        var model = new StudentTranscriptViewModel
        {
            StudentId = student.Id,
            SelectedSemesterId = targetSemesterId,
            SelectedSubjectId = subjectId,
            AvailableSemesters = semestersList
        };

        if (targetSemesterId > 0)
        {
            var transcriptResult = await _scoreService.GetStudentTranscriptAsync(student.Id, targetSemesterId, student.UserId);
            if (transcriptResult.Success)
            {
                model.Transcript = transcriptResult.Data;
                if (subjectId.HasValue &&
                    !(model.Transcript?.Subjects.Any(s => s.SubjectId == subjectId.Value) ?? false))
                {
                    model.SelectedSubjectId = null;
                }
            }
            else
            {
                TempData["ErrorMessage"] = transcriptResult.Message;
            }
        }

        return View(model);
    }

    private async Task<HighSchoolManagementSystem.Models.Entities.Student?> ResolveCurrentStudentAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _studentRepository.GetByUserIdAsync(userId);
    }
}
