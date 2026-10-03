using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IStudentService
{
    Task<ServiceResult<List<Student>>> GetStudentsAsync(string? search = null, string? status = null, int? classId = null, bool unassignedOnly = false);
    Task<ServiceResult<Student>> GetStudentAsync(int id);
    Task<ServiceResult<Student>> CreateStudentAsync(CreateStudentDto dto);
    Task<ServiceResult<Student>> UpdateStudentAsync(int id, UpdateStudentDto dto);
    Task<ServiceResult<List<Class>>> GetCurrentClassesAsync();
    Task<ServiceResult<AcademicYear?>> GetCurrentYearAsync();
    Task<ServiceResult> AssignClassAsync(int studentId, int classId);
}
