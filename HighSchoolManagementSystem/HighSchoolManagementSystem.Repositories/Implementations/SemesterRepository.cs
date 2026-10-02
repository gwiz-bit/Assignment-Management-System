using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class SemesterRepository : ISemesterRepository
{
    private readonly AppDbContext _context;

    public SemesterRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Semester>> GetByAcademicYearIdAsync(int academicYearId)
    {
        return await _context.Semesters
            .AsNoTracking()
            .Include(s => s.AcademicYear)
            .Where(s => s.AcademicYearId == academicYearId)
            .OrderBy(s => s.StartDate)
            .ToListAsync();
    }

    public async Task<Semester?> GetByIdAsync(int id)
    {
        return await _context.Semesters
            .AsNoTracking()
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Semester?> GetTrackedByIdAsync(int id)
    {
        return await _context.Semesters
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ExistsBySemesterCodeAsync(string semesterCode, int? excludeId = null)
    {
        var normalized = semesterCode.Trim().ToLower();
        var query = _context.Semesters
            .Where(s => s.SemesterCode.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(Semester semester)
    {
        await _context.Semesters.AddAsync(semester);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
