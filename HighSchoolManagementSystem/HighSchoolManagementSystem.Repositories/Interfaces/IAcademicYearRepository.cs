using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IAcademicYearRepository
{
    Task<IReadOnlyList<AcademicYear>> GetAllAsync();
    Task<AcademicYear?> GetByIdAsync(int id);
}
