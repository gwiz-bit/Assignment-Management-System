using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IClassService
{
    Task<ServiceResult<IReadOnlyList<Class>>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId);
}
