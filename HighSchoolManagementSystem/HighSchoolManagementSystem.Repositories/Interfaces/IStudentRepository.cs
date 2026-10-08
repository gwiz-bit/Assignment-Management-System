using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public enum StudentAssignmentResult { Success, StudentNotFound, StudentInactive, ClassNotFound, YearNotCurrent, AlreadyAssigned }

public interface IStudentRepository
{
    Task<List<Student>> GetAllAsync(string? search, string? status, int? classId, bool unassignedOnly);
    Task<Student?> GetByIdAsync(int id);
    Task<Student?> GetByUserIdAsync(int userId);
    Task<bool> ExistsByCodeAsync(string code, int? excludedId = null);
    Task<Role?> GetStudentRoleAsync();
    Task AddAsync(Student student);
    Task SaveChangesAsync();
    Task<List<Class>> GetCurrentClassesAsync();
    Task<AcademicYear?> GetCurrentYearAsync();
    Task<StudentAssignmentResult> AssignClassAsync(int studentId, int classId);
}
