using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface ISemesterRepository
{
    Task<IReadOnlyList<Semester>> GetByAcademicYearIdAsync(int academicYearId);
    Task<Semester?> GetByIdAsync(int id);
    Task<Semester?> GetTrackedByIdAsync(int id);
    Task ClearActiveFlagsAsync();
    Task<bool> ExistsBySemesterCodeAsync(string semesterCode, int? excludeId = null);
    Task AddAsync(Semester semester);
    Task SaveChangesAsync();
}
