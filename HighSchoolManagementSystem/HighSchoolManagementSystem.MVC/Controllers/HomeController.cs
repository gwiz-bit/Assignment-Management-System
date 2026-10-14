using HighSchoolManagementSystem.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using HighSchoolManagementSystem.Repositories.Data;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
        _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PublicClasses()
        {
            var classes = await _db.TeachingAssignments.AsNoTracking()
                .Where(t => t.Status == "Assigned" && t.Semester.IsActive && t.Semester.AcademicYear.IsCurrent && t.Subject.IsActive)
                .Include(t => t.Class).ThenInclude(c => c.GradeLevel)
                .Include(t => t.Subject).Include(t => t.Teacher).ThenInclude(t => t.User)
                .Include(t => t.Semester)
                .OrderBy(t => t.Subject.SubjectName).ThenBy(t => t.Class.ClassName)
                .ToListAsync();
            return View(classes);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
