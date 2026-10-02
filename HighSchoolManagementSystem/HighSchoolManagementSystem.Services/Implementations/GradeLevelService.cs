using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class GradeLevelService : IGradeLevelService
{
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public GradeLevelService(IGradeLevelRepository gradeLevelRepository)
    {
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<GradeLevel>>> GetAllAsync()
    {
        try
        {
            var levels = await _gradeLevelRepository.GetAllAsync();
            return ServiceResult<IReadOnlyList<GradeLevel>>.Ok(levels);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<GradeLevel>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách khối lớp. Vui lòng thử lại sau.");
        }
    }
}
