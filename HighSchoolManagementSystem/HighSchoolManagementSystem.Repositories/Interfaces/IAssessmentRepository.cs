using HighSchoolManagementSystem.Models.Entities;

namespace HighSchoolManagementSystem.Repositories.Interfaces;

public interface IAssessmentRepository
{
    Task<Assessment?> GetByIdAsync(int id);
    Task<Assessment?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Assessment>> GetByAssignmentIdAsync(int assignmentId);
    Task<IEnumerable<Assessment>> GetByTeacherAsync(int teacherId, int? semesterId = null, int? classId = null);
    Task<int> CountByTypeAndAssignmentAsync(int assignmentId, int assessmentTypeId, int? excludeAssessmentId = null);
    Task<IEnumerable<AssessmentType>> GetAllAssessmentTypesAsync();
    Task<AssessmentType?> GetAssessmentTypeByIdAsync(int id);
    Task<bool> AssessmentTypeCodeExistsAsync(string code, int? excludedId = null);
    Task<decimal> GetAssessmentTypeWeightTotalAsync(int? excludedId = null);
    Task AddAssessmentTypeAsync(AssessmentType type);
    Task UpdateAssessmentTypeAsync(AssessmentType type);
    Task DeleteAssessmentTypeAsync(AssessmentType type);
    Task<TeachingAssignment?> GetTeachingAssignmentByIdAsync(int assignmentId);
    Task<IEnumerable<TeachingAssignment>> GetTeachingAssignmentsByTeacherAsync(int teacherId, int? semesterId = null);
    Task<bool> HasLockedScoresAsync(int assessmentId);
    Task AddAsync(Assessment assessment);
    Task UpdateAsync(Assessment assessment);
    Task RecalculateSubjectResultsAsync(Assessment assessment);
    Task DeleteAsync(Assessment assessment);
    Task SaveChangesAsync();
}
