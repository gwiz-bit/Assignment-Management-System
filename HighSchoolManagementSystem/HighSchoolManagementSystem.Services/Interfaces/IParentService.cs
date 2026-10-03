using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IParentService
{
    Task<ServiceResult<List<Parent>>> GetParentsAsync(string? search = null, bool? isActive = null);
    Task<ServiceResult<Parent>> GetParentAsync(int id);
    Task<ServiceResult<Parent>> CreateParentAsync(CreateParentDto dto);
    Task<ServiceResult<Parent>> UpdateParentAsync(int id, UpdateParentDto dto);
    Task<ServiceResult<List<Student>>> GetAvailableStudentsAsync(string? search = null);
    Task<ServiceResult> LinkStudentsAsync(int parentId, IEnumerable<int>? studentIds);
}
