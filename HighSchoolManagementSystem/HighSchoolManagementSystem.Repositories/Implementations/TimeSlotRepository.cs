using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class TimeSlotRepository : ITimeSlotRepository
{
    private readonly AppDbContext _context;

    public TimeSlotRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TimeSlot>> GetByFiltersAsync(string? session)
    {
        var query = _context.TimeSlots.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(session))
        {
            query = query.Where(t => t.Session == session);
        }

        return await query
            .OrderBy(t => t.PeriodNo)
            .ThenBy(t => t.StartTime)
            .ToListAsync();
    }
}
