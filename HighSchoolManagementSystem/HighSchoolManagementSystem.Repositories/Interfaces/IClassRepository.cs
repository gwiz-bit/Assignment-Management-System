using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IClassRepository
{
    Task<IReadOnlyList<Class>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId);
}
