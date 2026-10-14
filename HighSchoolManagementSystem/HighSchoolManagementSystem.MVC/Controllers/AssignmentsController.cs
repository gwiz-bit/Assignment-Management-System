using System.Security.Claims;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.MVC.Models.ViewModels;
using HighSchoolManagementSystem.Repositories.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.MVC.Controllers;

[Authorize]
public class AssignmentsController(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> PublicTemplates()
    {
        var templates = await db.AssignmentTemplates.AsNoTracking()
            .Where(a => a.IsPublished && a.Subject.IsActive)
            .Include(a => a.Subject)
            .OrderBy(a => a.Subject.SubjectName).ThenBy(a => a.Title)
            .ToListAsync();
        return View("Templates", templates);
    }

    [Authorize(Roles = "Expert,Teacher,Admin")]
    [HttpGet]
    public async Task<IActionResult> Templates()
    {
        var query = db.AssignmentTemplates.AsNoTracking().Include(a => a.Subject).AsQueryable();
        if (User.IsInRole("Teacher"))
        {
            var teacherId = await TeacherIdAsync();
            if (teacherId == null) return Forbid();
            var subjectIds = db.TeachingAssignments.Where(t => t.TeacherId == teacherId).Select(t => t.SubjectId);
            query = query.Where(a => subjectIds.Contains(a.SubjectId));
        }
        return View(await query.OrderBy(a => a.Subject.SubjectName).ThenBy(a => a.Title).ToListAsync());
    }

    [Authorize(Roles = "Expert,Admin")]
    [HttpGet]
    public async Task<IActionResult> CreateTemplate()
    {
        ViewBag.Subjects = await db.Subjects.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SubjectName).ToListAsync();
        return View(new AssignmentTemplateFormViewModel());
    }

    [Authorize(Roles = "Expert,Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(AssignmentTemplateFormViewModel model, bool publish = false)
    {
        if (!await db.Subjects.AnyAsync(s => s.Id == model.SubjectId && s.IsActive))
            ModelState.AddModelError(nameof(model.SubjectId), "Môn học không tồn tại hoặc đã ngừng hoạt động.");
        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = await db.Subjects.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SubjectName).ToListAsync();
            return View(model);
        }
        db.AssignmentTemplates.Add(new AssignmentTemplate
        {
            Title = model.Title.Trim(), Description = model.Description?.Trim(), SubjectId = model.SubjectId,
            CreatedByUserId = CurrentUserId(), IsPublished = publish
        });
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = publish ? "Đã tạo và công khai mẫu bài tập." : "Đã lưu mẫu bài tập.";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> MyAssignments()
    {
        var student = await db.Students.Include(s => s.StudentClasses)
            .ThenInclude(sc => sc.Class).ThenInclude(c => c.TeachingAssignments)
            .SingleOrDefaultAsync(s => s.UserId == CurrentUserId());
        if (student == null) return Forbid();
        var subjectIds = student.StudentClasses.Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
            .SelectMany(sc => sc.Class.TeachingAssignments).Select(t => t.SubjectId).Distinct().ToList();
        var templates = await db.AssignmentTemplates.AsNoTracking().Include(a => a.Subject)
            .Where(a => a.IsPublished && subjectIds.Contains(a.SubjectId)).OrderBy(a => a.Title).ToListAsync();
        ViewBag.SubmissionsByAssignment = await db.AssignmentSubmissions.AsNoTracking()
            .Where(s => s.StudentId == student.Id).ToDictionaryAsync(s => s.AssignmentTemplateId);
        return View(templates);
    }

    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> Practice()
    {
        var subjects = await CurrentStudentSubjectsAsync();
        ViewBag.Subjects = subjects;
        return View(new PracticeRequestViewModel { SubjectId = subjects.FirstOrDefault()?.Id ?? 0 });
    }

    [Authorize(Roles = "Student")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Practice(PracticeRequestViewModel model)
    {
        var subjects = await CurrentStudentSubjectsAsync();
        ViewBag.Subjects = subjects;
        if (!subjects.Any(s => s.Id == model.SubjectId)) return Forbid();
        if (!ModelState.IsValid) return View(model);
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ModelState.AddModelError(string.Empty, "Chức năng AI chưa được cấu hình. Hãy đặt Gemini:ApiKey trong User Secrets hoặc biến môi trường.");
            return View(model);
        }
        var modelName = configuration["Gemini:Model"] ?? "gemini-3.8-flash";
        var prompt = $"Create a Vietnamese practice set for a high school student. Subject: {subjects.Single(s => s.Id == model.SubjectId).SubjectName}. Topic: {model.Topic.Trim()}. Return 5 varied questions from basic to advanced, clear instructions, and put a separate answer key at the end. Keep it age appropriate.";
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(modelName)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = System.Net.Http.Json.JsonContent.Create(new { contents = new[] { new { parts = new[] { new { text = prompt } } } } });
        try
        {
            using var response = await httpClientFactory.CreateClient().SendAsync(request);
            var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>();
            var generatedText = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(generatedText))
                ModelState.AddModelError(string.Empty, "Gemini không thể tạo bài luyện tập. Kiểm tra cấu hình API và thử lại.");
            else model.GeneratedPractice = generatedText;
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối Gemini. Vui lòng thử lại sau.");
        }
        return View(model);
    }

    [Authorize(Roles = "Student")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(AssignmentSubmissionFormViewModel model)
    {
        var userId = CurrentUserId();
        var student = await db.Students.Include(s => s.StudentClasses).ThenInclude(sc => sc.Class)
            .ThenInclude(c => c.TeachingAssignments).SingleOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Forbid();
        var template = await db.AssignmentTemplates.SingleOrDefaultAsync(a => a.Id == model.AssignmentId && a.IsPublished);
        if (template == null) return NotFound();
        var eligible = student.StudentClasses.Where(sc => sc.Status == "Enrolled" && sc.LeftAt == null)
            .SelectMany(sc => sc.Class.TeachingAssignments).Any(t => t.SubjectId == template.SubjectId);
        if (!eligible) return Forbid();
        if (!ModelState.IsValid) return RedirectToAction(nameof(MyAssignments));
        var submission = await db.AssignmentSubmissions.SingleOrDefaultAsync(s => s.StudentId == student.Id && s.AssignmentTemplateId == template.Id);
        if (submission?.EvaluatedAt != null)
        {
            TempData["ErrorMessage"] = "Bài đã được chấm và không thể sửa.";
            return RedirectToAction(nameof(MyAssignments));
        }
        if (submission == null)
        {
            submission = new AssignmentSubmission { StudentId = student.Id, AssignmentTemplateId = template.Id };
            db.AssignmentSubmissions.Add(submission);
        }
        submission.Answer = model.Answer.Trim();
        submission.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã nộp bài tập.";
        return RedirectToAction(nameof(MyAssignments));
    }

    [Authorize(Roles = "Teacher,Expert,Admin")]
    [HttpGet]
    public async Task<IActionResult> Submissions(int? assignmentId)
    {
        var query = db.AssignmentSubmissions.Include(s => s.AssignmentTemplate).ThenInclude(a => a.Subject)
            .Include(s => s.Student).ThenInclude(st => st.User).AsQueryable();
        if (User.IsInRole("Teacher"))
        {
            var teacherId = await TeacherIdAsync();
            if (teacherId == null) return Forbid();
            var subjectIds = db.TeachingAssignments.Where(t => t.TeacherId == teacherId).Select(t => t.SubjectId);
            query = query.Where(s => subjectIds.Contains(s.AssignmentTemplate.SubjectId));
        }
        if (assignmentId.HasValue) query = query.Where(s => s.AssignmentTemplateId == assignmentId.Value);
        return View(await query.OrderByDescending(s => s.SubmittedAt).ToListAsync());
    }

    [Authorize(Roles = "Teacher,Expert,Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Grade(AssignmentGradeViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Submissions));
        var submission = await db.AssignmentSubmissions.Include(s => s.AssignmentTemplate)
            .SingleOrDefaultAsync(s => s.Id == model.SubmissionId);
        if (submission == null) return NotFound();
        if (User.IsInRole("Teacher"))
        {
            var teacherId = await TeacherIdAsync();
            if (teacherId == null || !await db.TeachingAssignments.AnyAsync(t => t.TeacherId == teacherId && t.SubjectId == submission.AssignmentTemplate.SubjectId)) return Forbid();
        }
        submission.Score = model.Score;
        submission.TeacherFeedback = model.TeacherFeedback?.Trim();
        submission.EvaluatedByUserId = CurrentUserId();
        submission.EvaluatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã lưu kết quả đánh giá.";
        return RedirectToAction(nameof(Submissions));
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task<int?> TeacherIdAsync() => await db.Teachers.Where(t => t.UserId == CurrentUserId()).Select(t => (int?)t.Id).SingleOrDefaultAsync();

    private async Task<List<Subject>> CurrentStudentSubjectsAsync()
    {
        var studentId = await db.Students.Where(s => s.UserId == CurrentUserId()).Select(s => (int?)s.Id).SingleOrDefaultAsync();
        if (studentId == null) return new();
        return await db.StudentClasses.Where(sc => sc.StudentId == studentId && sc.Status == "Enrolled" && sc.LeftAt == null)
            .SelectMany(sc => sc.Class.TeachingAssignments).Select(t => t.Subject).Distinct()
            .OrderBy(s => s.SubjectName).ToListAsync();
    }

    private sealed record GeminiGenerateResponse(List<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
    private sealed record GeminiContent(List<GeminiPart>? Parts);
    private sealed record GeminiPart(string? Text);
}
