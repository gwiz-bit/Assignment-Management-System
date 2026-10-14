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
            var classes = await _db.Classes.AsNoTracking()
                .Where(c => c.AcademicYear.IsCurrent)
                .Include(c => c.AcademicYear).Include(c => c.GradeLevel)
                .Include(c => c.TeachingAssignments).ThenInclude(t => t.Subject)
                .OrderBy(c => c.GradeLevel.LevelName).ThenBy(c => c.ClassName)
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
