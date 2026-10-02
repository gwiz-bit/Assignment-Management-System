using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IGradeLevelRepository
{
    Task<IReadOnlyList<GradeLevel>> GetAllAsync();
    Task<GradeLevel?> GetByIdAsync(int id);
}
