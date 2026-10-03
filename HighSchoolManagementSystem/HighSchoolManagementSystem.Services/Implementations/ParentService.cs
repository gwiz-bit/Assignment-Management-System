using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Security;
using Microsoft.Extensions.Logging;

namespace HighSchoolManagementSystem.Services.Implementations;

public class ParentService(IParentRepository parents, IUserRepository users, IPasswordHasher passwordHasher,
    ILogger<ParentService> logger) : IParentService
{
    public const string NotFound = "PARENT_NOT_FOUND";

    public Task<ServiceResult<List<Parent>>> GetParentsAsync(string? search = null, bool? isActive = null) =>
        ExecuteAsync(async () => ServiceResult<List<Parent>>.Ok(await parents.GetAllAsync(search, isActive)));

    public Task<ServiceResult<Parent>> GetParentAsync(int id) => ExecuteAsync(async () =>
    {
        var parent = await parents.GetByIdAsync(id);
        return parent == null ? ServiceResult<Parent>.Fail(NotFound, "Không tìm thấy phụ huynh.") : ServiceResult<Parent>.Ok(parent);
    });

    public Task<ServiceResult<Parent>> CreateParentAsync(CreateParentDto dto) => ExecuteAsync(async () =>
    {
        var error = Validate(dto);
        if (error != null) return ServiceResult<Parent>.Fail(ErrorCodes.ValidationFailed, error);
        var username = dto.Username.Trim().ToLowerInvariant();
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await users.ExistsByUsernameAsync(username))
            return ServiceResult<Parent>.Fail(ErrorCodes.AuthUsernameDuplicate, "Tên đăng nhập đã được sử dụng.");
        if (await users.ExistsByEmailAsync(email))
            return ServiceResult<Parent>.Fail(ErrorCodes.UserEmailDuplicate, "Email đã được sử dụng.");
        var role = await parents.GetParentRoleAsync();
        if (role == null) return ServiceResult<Parent>.Fail(ErrorCodes.UserRoleInvalid, "Chưa cấu hình vai trò Parent.");
        var parent = new Parent
        {
            User = new User
            {
                Username = username, PasswordHash = passwordHasher.HashPassword(dto.Password),
                RoleId = role.RoleId, Role = role, IsActive = true, CreateAt = DateTime.UtcNow
            }
        };
        Apply(parent, dto);
        await parents.AddAsync(parent);
        // EF saves the new User and Parent together atomically; no orphan account on failure.
        await parents.SaveChangesAsync();
        return ServiceResult<Parent>.Ok(parent, "Đã tạo hồ sơ và tài khoản phụ huynh.");
    });

    public Task<ServiceResult<Parent>> UpdateParentAsync(int id, UpdateParentDto dto) => ExecuteAsync(async () =>
    {
        var error = Validate(dto);
        if (error != null) return ServiceResult<Parent>.Fail(ErrorCodes.ValidationFailed, error);
        var parent = await parents.GetByIdAsync(id);
        if (parent == null) return ServiceResult<Parent>.Fail(NotFound, "Không tìm thấy phụ huynh.");
        if (await users.ExistsByEmailAsync(dto.Email.Trim().ToLowerInvariant(), parent.UserId))
            return ServiceResult<Parent>.Fail(ErrorCodes.UserEmailDuplicate, "Email đã được sử dụng.");
        Apply(parent, dto);
        await parents.SaveChangesAsync();
        return ServiceResult<Parent>.Ok(parent, "Đã cập nhật thông tin phụ huynh.");
    });

    public Task<ServiceResult<List<Student>>> GetAvailableStudentsAsync(string? search = null) =>
        ExecuteAsync(async () => ServiceResult<List<Student>>.Ok(await parents.GetAvailableStudentsAsync(search)));

    public async Task<ServiceResult> LinkStudentsAsync(int parentId, IEnumerable<int>? studentIds)
    {
        var ids = studentIds?.Distinct().ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0 || ids.Any(id => id <= 0))
            return ServiceResult.Fail(ErrorCodes.ValidationFailed, "Vui lòng chọn ít nhất một học sinh hợp lệ.");
        try
        {
            return await parents.LinkStudentsAsync(parentId, ids) switch
            {
                ParentLinkResult.Success => ServiceResult.Ok("Đã liên kết học sinh với phụ huynh."),
                ParentLinkResult.ParentNotFound => ServiceResult.Fail(NotFound, "Không tìm thấy phụ huynh."),
                ParentLinkResult.AccountNotAuthorized => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Chỉ liên kết với tài khoản đang hoạt động và có vai trò Parent."),
                ParentLinkResult.StudentNotFound => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Có học sinh không còn tồn tại. Vui lòng tải lại danh sách."),
                _ => ServiceResult.Fail(ErrorCodes.ValidationFailed, "Có học sinh đã được liên kết với phụ huynh khác. Không có thay đổi nào được lưu.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể liên kết học sinh với phụ huynh {ParentId}", parentId);
            return ServiceResult.Fail(ErrorCodes.DatabaseError, "Không thể lưu liên kết. Vui lòng tải lại danh sách và thử lại.");
        }
    }

    private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> action)
    {
        try { return await action(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xử lý thông tin phụ huynh");
            return ServiceResult<T>.Fail(ErrorCodes.DatabaseError, "Không thể xử lý dữ liệu phụ huynh. Vui lòng tải lại trang và thử lại.");
        }
    }

    private static string? Validate(UpdateParentDto? dto)
    {
        if (dto == null) return "Dữ liệu phụ huynh không hợp lệ.";
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        if (dto is CreateParentDto create && System.Text.Encoding.UTF8.GetByteCount(create.Password ?? "") > 72)
            return "Mật khẩu không được vượt quá 72 byte UTF-8.";
        return results.Count == 0 ? null : string.Join(" ", results.Select(r => r.ErrorMessage));
    }

    private static void Apply(Parent parent, UpdateParentDto dto)
    {
        parent.User.FullName = dto.FullName.Trim();
        parent.User.Email = dto.Email.Trim().ToLowerInvariant();
        parent.User.PhoneNumber = Clean(dto.PhoneNumber);
        parent.User.Gender = Clean(dto.Gender);
        parent.User.DateOfBirth = dto.DateOfBirth?.Date;
        parent.Occupation = Clean(dto.Occupation);
        parent.EmergencyContact = Clean(dto.EmergencyContact);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
