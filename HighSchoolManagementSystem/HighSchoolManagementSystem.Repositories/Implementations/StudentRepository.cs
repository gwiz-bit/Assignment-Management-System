using System.Data;
using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Data;
using HighSchoolManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HighSchoolManagementSystem.Repositories.Implementations;

public class StudentRepository(AppDbContext context) : IStudentRepository
{
    private IQueryable<Student> WithDetails() => context.Students
        .Include(s => s.User).ThenInclude(u => u.Role)
        .Include(s => s.Parent).ThenInclude(p => p!.User)
        .Include(s => s.StudentClasses.Where(sc => sc.Class.AcademicYear.IsCurrent))
            .ThenInclude(sc => sc.Class).ThenInclude(c => c.AcademicYear)
        .Include(s => s.StudentClasses.Where(sc => sc.Class.AcademicYear.IsCurrent))
            .ThenInclude(sc => sc.Class).ThenInclude(c => c.GradeLevel);

    public async Task<List<Student>> GetAllAsync(string? search, string? status, int? classId, bool unassignedOnly)
    {
        var query = WithDetails().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s => s.StudentCode.ToLower().Contains(term) || s.User.FullName.ToLower().Contains(term)
                || s.User.Username.ToLower().Contains(term) || s.User.Email.ToLower().Contains(term)
                || (s.User.PhoneNumber != null && s.User.PhoneNumber.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (classId.HasValue) query = query.Where(s => s.StudentClasses.Any(sc => sc.ClassId == classId
            && sc.Class.AcademicYear.IsCurrent && sc.Status == "Enrolled" && sc.LeftAt == null));
        if (unassignedOnly) query = query.Where(s => !s.StudentClasses.Any(sc => sc.Class.AcademicYear.IsCurrent));
        return await query.OrderBy(s => s.StudentCode).ToListAsync();
    }

    public Task<Student?> GetByIdAsync(int id) => WithDetails().FirstOrDefaultAsync(s => s.Id == id);
    public Task<bool> ExistsByCodeAsync(string code, int? excludedId = null) => context.Students
        .AnyAsync(s => s.StudentCode.ToLower() == code.ToLower() && (!excludedId.HasValue || s.Id != excludedId.Value));
    public Task<Role?> GetStudentRoleAsync() => context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
    public async Task AddAsync(Student student) => await context.Students.AddAsync(student);
    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
    public Task<AcademicYear?> GetCurrentYearAsync() => context.AcademicYears.AsNoTracking().FirstOrDefaultAsync(y => y.IsCurrent);
    public Task<List<Class>> GetCurrentClassesAsync() => context.Classes.AsNoTracking()
        .Include(c => c.AcademicYear).Include(c => c.GradeLevel)
        .Where(c => c.AcademicYear.IsCurrent).OrderBy(c => c.GradeLevel.LevelCode).ThenBy(c => c.ClassName).ToListAsync();

    public async Task<StudentAssignmentResult> AssignClassAsync(int studentId, int classId)
    {
        // Serialize decisions for the same student, including simultaneous assignments to different classes.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var student = await context.Students.FromSqlInterpolated($"SELECT * FROM students WITH (UPDLOCK, HOLDLOCK) WHERE id = {studentId}")
            .AsNoTracking().SingleOrDefaultAsync();
        if (student == null) return StudentAssignmentResult.StudentNotFound;
        if (student.Status != "Active") return StudentAssignmentResult.StudentInactive;
        var target = await context.Classes.AsNoTracking().Include(c => c.AcademicYear).SingleOrDefaultAsync(c => c.Id == classId);
        if (target == null) return StudentAssignmentResult.ClassNotFound;
        if (!target.AcademicYear.IsCurrent) return StudentAssignmentResult.YearNotCurrent;
        // Even a closed membership prevents this endpoint being used as a class-transfer operation.
        if (await context.StudentClasses.AnyAsync(sc => sc.StudentId == studentId && sc.Class.AcademicYearId == target.AcademicYearId))
            return StudentAssignmentResult.AlreadyAssigned;
        context.StudentClasses.Add(new StudentClass { StudentId = studentId, ClassId = classId, Status = "Enrolled", JoinedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return StudentAssignmentResult.Success;
    }
}
