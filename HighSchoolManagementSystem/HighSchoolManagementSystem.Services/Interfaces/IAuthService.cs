using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Services.Common;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<User>> AuthenticateAsync(string usernameOrEmail, string password);
}
