using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Admin
{
    public interface IClassRepository
    {
        Task<List<SchoolClass>> GetAllClassesAsync();
        Task<SchoolClass?> GetClassByIdAsync(int id);
        Task<bool> ClassNameExistsAsync(string className);
        Task<bool> ClassNameExistsExceptIdAsync(string className, int id);
        Task AddClassAsync(SchoolClass schoolClass);
        Task UpdateClassAsync(SchoolClass schoolClass);
        Task DeleteClassAsync(SchoolClass schoolClass);
        Task<SchoolClass?> GetClassWithStudentsAndExercisesAsync(int id);
        Task<Dictionary<int, int>> GetSubmissionCountsByStudentIdsAsync(List<int> studentIds);
        Task<bool> IsStudentInClassAsync(int classId, int studentId);
        Task AddStudentToClassAsync(StudentClass studentClass);
        Task<StudentClass?> GetStudentClassByIdAsync(int studentClassId);
        Task RemoveStudentFromClassAsync(StudentClass record);
        Task<ClassExercise?> GetClassExerciseByIdAsync(int classExerciseId);
        Task UpdateClassExerciseAsync(ClassExercise assignment);
        Task<List<ClassExercise>> GetAssignedExercisesByClassIdAsync(int classId);
    }
}
