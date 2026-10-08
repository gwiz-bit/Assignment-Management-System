using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ITimetableService
{
    Task<ServiceResult<IReadOnlyList<TimetableEntry>>> GetAllAsync(int? semesterId = null);
    Task<ServiceResult<TimetableEntry>> GetByIdAsync(int id);
    Task<ServiceResult<TimetableEntry>> SaveAsync(int? id, int assignmentId, int roomId, int timeSlotId, int dayOfWeek);
    Task<ServiceResult<bool>> DeleteAsync(int id);
    Task<ServiceResult<IReadOnlyList<TeachingAssignment>>> GetAssignmentsAsync(int? semesterId = null);
    Task<ServiceResult<IReadOnlyList<Room>>> GetRoomsAsync();
    Task<ServiceResult<IReadOnlyList<TimeSlot>>> GetTimeSlotsAsync();
    Task<int?> GetActiveSemesterIdAsync();
    Task<ServiceResult<IReadOnlyList<TimetableEntry>>> GetForCurrentUserAsync(string role, int userId, int? semesterId = null);
    Task<ServiceResult<IReadOnlyList<TimetableEntry>>> GetForClassAsync(int classId, int? semesterId = null);
}
