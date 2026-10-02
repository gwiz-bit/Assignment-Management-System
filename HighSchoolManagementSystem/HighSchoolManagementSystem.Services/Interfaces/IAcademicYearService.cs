using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAcademicYearService
{
    Task<ServiceResult<IReadOnlyList<AcademicYear>>> GetAllAsync();
}
