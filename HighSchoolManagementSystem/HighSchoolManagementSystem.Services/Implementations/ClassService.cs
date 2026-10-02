using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;
    private readonly IAcademicYearRepository _academicYearRepository;
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public ClassService(
        IClassRepository classRepository,
        IAcademicYearRepository academicYearRepository,
        IGradeLevelRepository gradeLevelRepository)
    {
        _classRepository = classRepository;
        _academicYearRepository = academicYearRepository;
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<Class>>> GetByFiltersAsync(int? academicYearId, int? gradeLevelId)
    {
        try
        {
            if (academicYearId.HasValue)
            {
                var year = await _academicYearRepository.GetByIdAsync(academicYearId.Value);
                if (year == null)
                {
                    return ServiceResult<IReadOnlyList<Class>>.Fail(
                        ErrorCodes.ResourceNotFound,
                        "Không tìm thấy năm học đã chọn.");
                }
            }

            if (gradeLevelId.HasValue)
            {
                var grade = await _gradeLevelRepository.GetByIdAsync(gradeLevelId.Value);
                if (grade == null)
                {
                    return ServiceResult<IReadOnlyList<Class>>.Fail(
                        ErrorCodes.ResourceNotFound,
                        "Không tìm thấy khối lớp đã chọn.");
                }
            }

            var classes = await _classRepository.GetByFiltersAsync(academicYearId, gradeLevelId);
            return ServiceResult<IReadOnlyList<Class>>.Ok(classes);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Class>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách lớp học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Class>> GetByIdWithDetailsAsync(int id)
    {
        try
        {
            var classEntity = await _classRepository.GetByIdWithDetailsAsync(id);
            if (classEntity == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ClassNotFound,
                    "Không tìm thấy lớp học.");
            }

            return ServiceResult<Class>.Ok(classEntity);
        }
        catch
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải thông tin lớp học. Vui lòng thử lại sau.");
        }
    }
}
