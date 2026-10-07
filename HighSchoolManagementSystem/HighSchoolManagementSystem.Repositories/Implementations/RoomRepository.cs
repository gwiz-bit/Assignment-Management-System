using Microsoft.EntityFrameworkCore;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Room>> GetByFiltersAsync(string? status, string? roomType)
    {
        var query = _context.Rooms.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(roomType))
        {
            query = query.Where(r => r.RoomType == roomType);
        }

        return await query
            .OrderBy(r => r.Building)
            .ThenBy(r => r.RoomCode)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetDistinctRoomTypesAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .Select(r => r.RoomType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }
}
