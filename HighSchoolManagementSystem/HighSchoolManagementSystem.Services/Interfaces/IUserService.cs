using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IUserService
{
    Task<ServiceResult<List<User>>> GetUsersAsync(string? search = null, int? roleId = null, bool? isActive = null);
    Task<ServiceResult<List<Role>>> GetRolesAsync();
    Task<ServiceResult<User>> GetUserByIdAsync(int userId);
    Task<ServiceResult<User>> CreateUserAsync(CreateUserDto dto);
    Task<ServiceResult<User>> UpdateUserAccountAsync(int userId, UpdateUserAccountDto dto);
    Task<ServiceResult<bool>> SetUserActiveAsync(int userId, bool isActive);
    Task<ServiceResult<bool>> ResetUserPasswordAsync(int userId, ResetUserPasswordDto dto);
    Task<ServiceResult<User>> GetUserProfileAsync(int userId);
    Task<ServiceResult<User>> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<ServiceResult<bool>> ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
