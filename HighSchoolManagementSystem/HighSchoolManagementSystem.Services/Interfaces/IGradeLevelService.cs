using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IGradeLevelService
{
    Task<ServiceResult<IReadOnlyList<GradeLevel>>> GetAllAsync();
}
