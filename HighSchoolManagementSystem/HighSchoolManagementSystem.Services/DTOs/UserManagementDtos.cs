using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.Services.DTOs;

public class CreateUserDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserAccountDto
{
    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public int RoleId { get; set; }
}

public class ResetUserPasswordDto
{
    [Required, StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
