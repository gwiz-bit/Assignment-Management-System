using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Auth;

public class RegisterViewModel
{
    [Required, StringLength(50), RegularExpression("^[a-zA-Z0-9_.-]+$")]
    public string Username { get; set; } = "";
    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
