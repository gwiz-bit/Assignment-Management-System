using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class AcademicYearRepository : IAcademicYearRepository
{
    private readonly AppDbContext _context;

    public AcademicYearRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AcademicYear>> GetAllAsync()
    {
        return await _context.AcademicYears
            .AsNoTracking()
            .Include(y => y.Semesters)
            .Include(y => y.Classes)
            .OrderByDescending(y => y.StartDate)
            .ToListAsync();
    }

    public async Task<AcademicYear?> GetByIdAsync(int id)
    {
        return await _context.AcademicYears
            .AsNoTracking()
            .Include(y => y.Semesters)
            .Include(y => y.Classes)
            .FirstOrDefaultAsync(y => y.Id == id);
    }

    public async Task<AcademicYear?> GetTrackedByIdAsync(int id)
    {
        return await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Id == id);
    }

    public async Task<bool> ExistsByYearCodeAsync(string yearCode, int? excludeId = null)
    {
        var normalized = yearCode.Trim().ToLower();
        var query = _context.AcademicYears
            .Where(y => y.YearCode.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(y => y.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(AcademicYear academicYear)
    {
        await _context.AcademicYears.AddAsync(academicYear);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
