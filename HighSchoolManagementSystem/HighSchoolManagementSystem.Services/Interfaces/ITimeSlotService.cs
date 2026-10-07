using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ITimeSlotService
{
    Task<ServiceResult<IReadOnlyList<TimeSlot>>> GetByFiltersAsync(string? session);
}
