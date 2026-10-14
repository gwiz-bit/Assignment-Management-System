using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HighSchoolManagementSystem.MVC.Models.ViewModels;

public class CreateSubjectClassViewModel
{
    [Range(1, int.MaxValue)] public int TeacherId { get; set; }
    [Range(1, int.MaxValue)] public int SubjectId { get; set; }
    [Range(1, int.MaxValue)] public int ClassId { get; set; }
    [Range(1, int.MaxValue)] public int SemesterId { get; set; }
    public List<SelectListItem> Teachers { get; set; } = new();
    public List<SelectListItem> Subjects { get; set; } = new();
    public List<SelectListItem> Classes { get; set; } = new();
    public List<SelectListItem> Semesters { get; set; } = new();
}
