using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ITimeSlotRepository
{
    Task<IReadOnlyList<TimeSlot>> GetByFiltersAsync(string? session);
}
