using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;
using HighSchoolManagementSystem.Services.Security;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<User>> AuthenticateAsync(string usernameOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
        {
            return ServiceResult<User>.Fail(ErrorCodes.ValidationFailed, "Vui lòng nhập đầy đủ thông tin đăng nhập.");
        }

        var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
        if (user == null)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthInvalidCredentials, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthAccountLocked, "Tài khoản của bạn đã bị khóa hoặc tạm dừng hoạt động. Vui lòng liên hệ nhà trường.");
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return ServiceResult<User>.Fail(ErrorCodes.AuthInvalidCredentials, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }

        return ServiceResult<User>.Ok(user, "Đăng nhập thành công.");
    }
}
