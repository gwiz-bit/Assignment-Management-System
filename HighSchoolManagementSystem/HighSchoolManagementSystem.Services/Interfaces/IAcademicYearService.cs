using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAcademicYearService
{
    Task<ServiceResult<IReadOnlyList<AcademicYear>>> GetAllAsync();
    Task<ServiceResult<AcademicYear>> CreateAsync(string yearCode, DateTime startDate, DateTime endDate);
}
