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

    public async Task<ServiceResult<AcademicYear>> CreateAsync(string yearCode, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(yearCode))
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.ValidationFailed,
                "Vui lòng nhập mã năm học.");
        }

        yearCode = yearCode.Trim();
        if (yearCode.Length > 20)
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.ValidationFailed,
                "Mã năm học không được vượt quá 20 ký tự.");
        }

        if (startDate.Date >= endDate.Date)
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.ValidationFailed,
                "Ngày kết thúc phải sau ngày bắt đầu.");
        }

        try
        {
            if (await _academicYearRepository.ExistsByYearCodeAsync(yearCode))
            {
                return ServiceResult<AcademicYear>.Fail(
                    ErrorCodes.ResourceAlreadyExists,
                    $"Mã năm học \"{yearCode}\" đã tồn tại trong hệ thống.");
            }

            var academicYear = new AcademicYear
            {
                YearCode = yearCode,
                StartDate = startDate.Date,
                EndDate = endDate.Date
            };

            await _academicYearRepository.AddAsync(academicYear);
            await _academicYearRepository.SaveChangesAsync();

            return ServiceResult<AcademicYear>.Ok(academicYear, "Tạo năm học thành công.");
        }
        catch
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tạo năm học. Vui lòng thử lại sau.");
        }
    }
}
