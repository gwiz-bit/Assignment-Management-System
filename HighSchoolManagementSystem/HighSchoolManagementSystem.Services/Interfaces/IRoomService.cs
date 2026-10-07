using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IRoomService
{
    Task<ServiceResult<IReadOnlyList<Room>>> GetByFiltersAsync(string? status, string? roomType);
    Task<ServiceResult<IReadOnlyList<string>>> GetRoomTypesAsync();
}
