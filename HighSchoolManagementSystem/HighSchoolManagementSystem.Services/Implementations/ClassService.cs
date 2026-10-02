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
    private readonly ITeacherRepository _teacherRepository;

    public ClassService(
        IClassRepository classRepository,
        IAcademicYearRepository academicYearRepository,
        IGradeLevelRepository gradeLevelRepository,
        ITeacherRepository teacherRepository)
    {
        _classRepository = classRepository;
        _academicYearRepository = academicYearRepository;
        _gradeLevelRepository = gradeLevelRepository;
        _teacherRepository = teacherRepository;
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

    public async Task<ServiceResult<Class>> CreateAsync(
        string className,
        int academicYearId,
        int gradeLevelId,
        int? homeroomTeacherId)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.ValidationFailed,
                "Vui lòng nhập tên lớp.");
        }

        className = className.Trim();
        if (className.Length > 50)
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.ValidationFailed,
                "Tên lớp không được vượt quá 50 ký tự.");
        }

        try
        {
            var year = await _academicYearRepository.GetByIdAsync(academicYearId);
            if (year == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học để tạo lớp.");
            }

            var grade = await _gradeLevelRepository.GetByIdAsync(gradeLevelId);
            if (grade == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy khối lớp đã chọn.");
            }

            if (homeroomTeacherId.HasValue)
            {
                var teacher = await _teacherRepository.GetByIdAsync(homeroomTeacherId.Value);
                if (teacher == null)
                {
                    return ServiceResult<Class>.Fail(
                        ErrorCodes.TeacherNotFound,
                        "Không tìm thấy giáo viên chủ nhiệm đã chọn.");
                }
            }

            if (await _classRepository.ExistsByNameInYearAsync(className, academicYearId))
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ResourceAlreadyExists,
                    $"Lớp \"{className}\" đã tồn tại trong năm học {year.YearCode}.");
            }

            var classEntity = new Class
            {
                ClassName = className,
                AcademicYearId = academicYearId,
                GradeLevelId = gradeLevelId,
                HomeroomTeacherId = homeroomTeacherId
            };

            await _classRepository.AddAsync(classEntity);
            await _classRepository.SaveChangesAsync();

            return ServiceResult<Class>.Ok(classEntity, "Tạo lớp học thành công.");
        }
        catch
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tạo lớp học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<Class>> UpdateAsync(
        int id,
        string className,
        int academicYearId,
        int gradeLevelId,
        int? homeroomTeacherId)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.ValidationFailed,
                "Vui lòng nhập tên lớp.");
        }

        className = className.Trim();
        if (className.Length > 50)
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.ValidationFailed,
                "Tên lớp không được vượt quá 50 ký tự.");
        }

        try
        {
            var classEntity = await _classRepository.GetTrackedByIdAsync(id);
            if (classEntity == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ClassNotFound,
                    "Không tìm thấy lớp học cần cập nhật.");
            }

            var year = await _academicYearRepository.GetByIdAsync(academicYearId);
            if (year == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy năm học đã chọn.");
            }

            var grade = await _gradeLevelRepository.GetByIdAsync(gradeLevelId);
            if (grade == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ResourceNotFound,
                    "Không tìm thấy khối lớp đã chọn.");
            }

            if (homeroomTeacherId.HasValue)
            {
                var teacher = await _teacherRepository.GetByIdAsync(homeroomTeacherId.Value);
                if (teacher == null)
                {
                    return ServiceResult<Class>.Fail(
                        ErrorCodes.TeacherNotFound,
                        "Không tìm thấy giáo viên chủ nhiệm đã chọn.");
                }
            }

            if (await _classRepository.ExistsByNameInYearAsync(className, academicYearId, excludeId: id))
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ResourceAlreadyExists,
                    $"Lớp \"{className}\" đã tồn tại trong năm học {year.YearCode}.");
            }

            classEntity.ClassName = className;
            classEntity.AcademicYearId = academicYearId;
            classEntity.GradeLevelId = gradeLevelId;
            classEntity.HomeroomTeacherId = homeroomTeacherId;

            await _classRepository.SaveChangesAsync();

            return ServiceResult<Class>.Ok(classEntity, "Cập nhật lớp học thành công.");
        }
        catch
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể cập nhật lớp học. Vui lòng thử lại sau.");
        }
    }

    public async Task<ServiceResult<IReadOnlyList<Class>>> GetAccessibleClassesForTeacherAsync(int teacherId)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                return ServiceResult<IReadOnlyList<Class>>.Fail(
                    ErrorCodes.TeacherNotFound,
                    "Không tìm thấy hồ sơ giáo viên.");
            }

            var classes = await _classRepository.GetAccessibleByTeacherIdAsync(teacherId);
            return ServiceResult<IReadOnlyList<Class>>.Ok(classes);
        }
        catch
        {
            return ServiceResult<IReadOnlyList<Class>>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách lớp của giáo viên.");
        }
    }

    public async Task<ServiceResult<Class>> GetClassStudentsForViewerAsync(int classId, int? teacherId = null)
    {
        try
        {
            var classEntity = await _classRepository.GetByIdWithDetailsAsync(classId);
            if (classEntity == null)
            {
                return ServiceResult<Class>.Fail(
                    ErrorCodes.ClassNotFound,
                    "Không tìm thấy lớp học.");
            }

            // teacherId == null → Staff/Principal (toàn quyền xem)
            if (teacherId.HasValue)
            {
                var authorized = await _classRepository.IsTeacherAuthorizedAsync(teacherId.Value, classId);
                if (!authorized)
                {
                    return ServiceResult<Class>.Fail(
                        ErrorCodes.AuthForbidden,
                        "Bạn không được phân công hoặc chủ nhiệm lớp này.");
                }
            }

            return ServiceResult<Class>.Ok(classEntity);
        }
        catch
        {
            return ServiceResult<Class>.Fail(
                ErrorCodes.DatabaseError,
                "Không thể tải danh sách học sinh của lớp.");
        }
    }
}
