using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IClassService
{
    Task<ServiceResult<IReadOnlyList<Class>>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId);
    Task<ServiceResult<Class>> GetByIdWithDetailsAsync(int id);
    Task<ServiceResult<Class>> CreateAsync(
        string className,
        int academicYearId,
        int gradeLevelId,
        int? homeroomTeacherId);
}
