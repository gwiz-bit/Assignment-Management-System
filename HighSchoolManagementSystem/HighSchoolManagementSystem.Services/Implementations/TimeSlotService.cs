using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class TimeSlotService : ITimeSlotService
{
    private readonly ITimeSlotRepository _timeSlotRepository;

    public TimeSlotService(ITimeSlotRepository timeSlotRepository)
    {
        _timeSlotRepository = timeSlotRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<TimeSlot>>> GetByFiltersAsync(string? session)
    {
        try
        {
            var slots = await _timeSlotRepository.GetByFiltersAsync(session);
            return ServiceResult<IReadOnlyList<TimeSlot>>.Ok(slots);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<TimeSlot>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách tiết học. Vui lòng thử lại sau.");
        }
    }
}
