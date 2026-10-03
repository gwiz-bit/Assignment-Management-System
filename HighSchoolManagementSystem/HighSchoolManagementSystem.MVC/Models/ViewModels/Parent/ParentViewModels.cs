using System.ComponentModel.DataAnnotations;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels.Parent;

public class CreateParentViewModel : CreateParentDto
{
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ParentListViewModel
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public List<ParentDetailsViewModel> Parents { get; set; } = new();
}

public class ParentDetailsViewModel : UpdateParentDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool CanLink => IsActive && RoleName == "Parent";
    public List<ParentStudentViewModel> Students { get; set; } = new();
}

public class ParentStudentViewModel
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ParentLinkViewModel
{
    public ParentDetailsViewModel Parent { get; set; } = new();
    public string? Search { get; set; }
    public List<int> SelectedStudentIds { get; set; } = new();
    public List<ParentStudentViewModel> AvailableStudents { get; set; } = new();
}
