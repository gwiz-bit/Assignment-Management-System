using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AcademicYearService : IAcademicYearService
{
    private readonly IAcademicYearRepository _academicYearRepository;

    public AcademicYearService(IAcademicYearRepository academicYearRepository)
    {
        _academicYearRepository = academicYearRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<AcademicYear>>> GetAllAsync()
    {
        try
        {
            var years = await _academicYearRepository.GetAllAsync();
            return ServiceResult<IReadOnlyList<AcademicYear>>.Ok(years);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<AcademicYear>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách năm học. Vui lòng thử lại sau.");
        }
    }
}
