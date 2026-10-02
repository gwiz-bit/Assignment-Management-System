using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAcademicYearService
{
    Task<ServiceResult<IReadOnlyList<AcademicYear>>> GetAllAsync();
    Task<ServiceResult<AcademicYear>> GetByIdAsync(int id);
    Task<ServiceResult<AcademicYear>> GetCurrentAsync();
    Task<ServiceResult<AcademicYear>> CreateAsync(string yearCode, DateTime startDate, DateTime endDate);
    Task<ServiceResult<AcademicYear>> UpdateAsync(int id, string yearCode, DateTime startDate, DateTime endDate);
    Task<ServiceResult<AcademicYear>> SetCurrentAsync(int id);
}
