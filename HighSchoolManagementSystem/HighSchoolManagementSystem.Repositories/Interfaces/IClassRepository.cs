using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IClassRepository
{
    Task<IReadOnlyList<Class>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId);
    Task<Class?> GetByIdWithDetailsAsync(int id);
    Task<bool> ExistsByNameInYearAsync(string className, int academicYearId, int? excludeId = null);
    Task AddAsync(Class classEntity);
    Task SaveChangesAsync();
}
