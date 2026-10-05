using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IClassRepository
{
    Task<IReadOnlyList<Class>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId);
    Task<Class?> GetByIdWithDetailsAsync(int id);
    Task<Class?> GetTrackedByIdAsync(int id);
    Task<bool> IsTeacherAuthorizedAsync(int teacherId, int classId);
    Task<IReadOnlyList<Class>> GetAccessibleByTeacherIdAsync(int teacherId);
    Task<IReadOnlyList<Class>> GetHomeroomClassesByTeacherIdAsync(int teacherId);
    Task<bool> ExistsByNameInYearAsync(string className, int academicYearId, int? excludeId = null);
    Task AddAsync(Class classEntity);
    Task SaveChangesAsync();
}
