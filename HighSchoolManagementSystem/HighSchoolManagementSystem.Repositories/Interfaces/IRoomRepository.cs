using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> GetByFiltersAsync(string? status, string? roomType);
    Task<IReadOnlyList<string>> GetDistinctRoomTypesAsync();
}
