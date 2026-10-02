using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface ISemesterService
{
    Task<ServiceResult<IReadOnlyList<Semester>>> GetByAcademicYearIdAsync(int academicYearId);
    Task<ServiceResult<Semester>> GetByIdAsync(int id);
    Task<ServiceResult<Semester>> CreateAsync(
        int academicYearId,
        string semesterCode,
        string semesterName,
        DateTime startDate,
        DateTime endDate);
    Task<ServiceResult<Semester>> UpdateAsync(
        int id,
        string semesterCode,
        string semesterName,
        DateTime startDate,
        DateTime endDate);
    Task<ServiceResult<Semester>> ActivateAsync(int id);
    Task<ServiceResult<Semester>> CloseAsync(int id);
}
