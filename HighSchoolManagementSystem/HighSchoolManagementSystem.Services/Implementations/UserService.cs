using System.Text.RegularExpressions;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ServiceResult<User>> GetUserProfileAsync(int userId)
    {
        if (userId <= 0)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Định danh người dùng không hợp lệ.");
        }

        var user = await _userRepository.GetUserFullProfileByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy thông tin người dùng.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa hoặc tạm dừng hoạt động.");
        }

        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<User>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        if (userId <= 0)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Định danh người dùng không hợp lệ.");
        }

        if (dto == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Dữ liệu cập nhật không hợp lệ.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy tài khoản người dùng.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa.");
        }

        // 1. Kiểm tra số điện thoại (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var cleanPhone = dto.PhoneNumber.Trim().Replace(" ", "").Replace(".", "").Replace("-", "");
            // Quy chuẩn SĐT Việt Nam: 10 chữ số, bắt đầu bằng 0
            if (!Regex.IsMatch(cleanPhone, @"^0[1-9]\d{8}$"))
            {
                return ServiceResult<User>.Fail(ErrorCodes.UserPhoneInvalid, "Số điện thoại không hợp lệ (phải gồm 10 chữ số và bắt đầu bằng số 0).");
            }
            user.PhoneNumber = cleanPhone;
        }
        else
        {
            user.PhoneNumber = null;
        }

        // 2. Kiểm tra ngày sinh (nếu có nhập)
        if (dto.DateOfBirth.HasValue)
        {
            if (dto.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
            {
                return ServiceResult<User>.Fail(ErrorCodes.UserDobInvalid, "Ngày sinh không thể lớn hơn hoặc bằng ngày hiện tại.");
            }
            if (dto.DateOfBirth.Value.Year < 1920)
            {
                return ServiceResult<User>.Fail(ErrorCodes.UserDobInvalid, "Năm sinh không hợp lệ.");
            }
            user.DateOfBirth = dto.DateOfBirth.Value.Date;
        }
        else
        {
            user.DateOfBirth = null;
        }

        // 3. Kiểm tra giới tính (nếu có nhập)
        if (!string.IsNullOrWhiteSpace(dto.Gender))
        {
            var validGenders = new[] { "Nam", "Nữ", "Khác" };
            var matchedGender = validGenders.FirstOrDefault(g => string.Equals(g, dto.Gender.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matchedGender == null)
            {
                return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Giới tính không hợp lệ (chỉ chấp nhận: Nam, Nữ, Khác).");
            }
            user.Gender = matchedGender;
        }
        else
        {
            user.Gender = null;
        }

        // 4. Cập nhật ảnh đại diện (nếu có đường dẫn mới)
        if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
        {
            user.AvatarUrl = dto.AvatarUrl.Trim();
        }

        try
        {
            await _userRepository.UpdateAsync(user);
            return ServiceResult<User>.Ok(user, "Cập nhật thông tin cá nhân thành công.");
        }
        catch (Exception ex)
        {
            return ServiceResult<User>.Fail(ErrorCodes.UserProfileUpdateFail, $"Lỗi cập nhật dữ liệu: {ex.Message}");
        }
    }
}
