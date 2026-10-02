using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class ClassRepository : IClassRepository
{
    private readonly AppDbContext _context;

    public ClassRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Class>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId)
    {
        var query = _context.Classes
            .AsNoTracking()
            .Include(c => c.AcademicYear)
            .Include(c => c.GradeLevel)
            .Include(c => c.HomeroomTeacher)!
                .ThenInclude(t => t!.User)
            .Include(c => c.StudentClasses)
            .AsQueryable();

        if (academicYearId.HasValue)
        {
            query = query.Where(c => c.AcademicYearId == academicYearId.Value);
        }

        if (gradeLevelId.HasValue)
        {
            query = query.Where(c => c.GradeLevelId == gradeLevelId.Value);
        }

        return await query
            .OrderBy(c => c.GradeLevel.LevelCode)
            .ThenBy(c => c.ClassName)
            .ToListAsync();
    }

    public async Task<Class?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Classes
            .AsNoTracking()
            .Include(c => c.AcademicYear)
            .Include(c => c.GradeLevel)
            .Include(c => c.HomeroomTeacher)!
                .ThenInclude(t => t!.User)
            .Include(c => c.TeachingAssignments)
                .ThenInclude(ta => ta.Teacher)
                    .ThenInclude(t => t.User)
            .Include(c => c.TeachingAssignments)
                .ThenInclude(ta => ta.Subject)
            .Include(c => c.TeachingAssignments)
                .ThenInclude(ta => ta.Semester)
            .Include(c => c.StudentClasses)
                .ThenInclude(sc => sc.Student)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Class?> GetTrackedByIdAsync(int id)
    {
        return await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExistsByNameInYearAsync(string className, int academicYearId, int? excludeId = null)
    {
        var normalized = className.Trim().ToLower();
        var query = _context.Classes
            .Where(c => c.AcademicYearId == academicYearId && c.ClassName.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(Class classEntity)
    {
        await _context.Classes.AddAsync(classEntity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
