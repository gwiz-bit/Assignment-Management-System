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

    public async Task<ServiceResult<AcademicYear>> GetByIdAsync(int id)
    {
        try
        {
            var year = await _academicYearRepository.GetByIdAsync(id);
            if (year == null)
            {
                return ServiceResult<AcademicYear>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học.");
            }

            return ServiceResult<AcademicYear>.Ok(year);
        }
        catch
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải thông tin năm học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<AcademicYear>> GetCurrentAsync()
    {
        try
        {
            var year = await _academicYearRepository.GetCurrentAsync();
            if (year == null)
            {
                return ServiceResult<AcademicYear>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Chưa chọn năm học hiện hành.");
            }

            return ServiceResult<AcademicYear>.Ok(year);
        }
        catch
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải năm học hiện hành. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<AcademicYear>> CreateAsync(string yearCode, DateTime startDate, DateTime endDate)
    {
        var validationError = ValidateInput(yearCode, startDate, endDate);
        if (validationError != null)
        {
            return validationError;
        }

        yearCode = yearCode.Trim();

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
                EndDate = endDate.Date,
                IsCurrent = false
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

    public async Task<ServiceResult<AcademicYear>> UpdateAsync(int id, string yearCode, DateTime startDate, DateTime endDate)
    {
        var validationError = ValidateInput(yearCode, startDate, endDate);
        if (validationError != null)
        {
            return validationError;
        }

        yearCode = yearCode.Trim();

        try
        {
            var academicYear = await _academicYearRepository.GetTrackedByIdAsync(id);
            if (academicYear == null)
            {
                return ServiceResult<AcademicYear>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học cần cập nhật.");
            }

            if (await _academicYearRepository.ExistsByYearCodeAsync(yearCode, excludeId: id))
            {
                return ServiceResult<AcademicYear>.Fail(
                    ErrorCodes.ResourceAlreadyExists,
                    $"Mã năm học \"{yearCode}\" đã tồn tại trong hệ thống.");
            }

            academicYear.YearCode = yearCode;
            academicYear.StartDate = startDate.Date;
            academicYear.EndDate = endDate.Date;

            await _academicYearRepository.SaveChangesAsync();

            return ServiceResult<AcademicYear>.Ok(academicYear, "Cập nhật năm học thành công.");
        }
        catch
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể cập nhật năm học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<AcademicYear>> SetCurrentAsync(int id)
    {
        try
        {
            var academicYear = await _academicYearRepository.GetTrackedByIdAsync(id);
            if (academicYear == null)
            {
                return ServiceResult<AcademicYear>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học cần đặt làm hiện hành.");
            }

            if (academicYear.IsCurrent)
            {
                return ServiceResult<AcademicYear>.Ok(academicYear, $"Năm học \"{academicYear.YearCode}\" đã là năm hiện hành.");
            }

            await _academicYearRepository.ClearCurrentFlagsAsync();

            academicYear.IsCurrent = true;
            await _academicYearRepository.SaveChangesAsync();

            return ServiceResult<AcademicYear>.Ok(
                academicYear,
                $"Đã chọn \"{academicYear.YearCode}\" làm năm học hiện hành.");
        }
        catch
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể đặt năm học hiện hành. Vui lòng thử lại sau.");
        }
    }

    private static ServiceResult<AcademicYear>? ValidateInput(string yearCode, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(yearCode))
        {
            return ServiceResult<AcademicYear>.Fail(
                ErrorCodes.ValidationFailed,
                "Vui lòng nhập mã năm học.");
        }

        if (yearCode.Trim().Length > 20)
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

        return null;
    }
}
