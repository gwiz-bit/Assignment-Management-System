using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class GradeLevelRepository : IGradeLevelRepository
{
    private readonly AppDbContext _context;

    public GradeLevelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GradeLevel>> GetAllAsync()
    {
        return await _context.GradeLevels
            .AsNoTracking()
            .OrderBy(g => g.LevelCode)
            .ToListAsync();
    }

    public async Task<GradeLevel?> GetByIdAsync(int id)
    {
        return await _context.GradeLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);
    }
}
