using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IAcademicYearRepository
{
    Task<IReadOnlyList<AcademicYear>> GetAllAsync();
    Task<AcademicYear?> GetByIdAsync(int id);
    Task<AcademicYear?> GetTrackedByIdAsync(int id);
    Task<bool> ExistsByYearCodeAsync(string yearCode, int? excludeId = null);
    Task AddAsync(AcademicYear academicYear);
    Task SaveChangesAsync();
}
