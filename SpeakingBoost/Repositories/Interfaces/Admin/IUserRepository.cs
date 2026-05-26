using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Admin
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllStudentsAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EmailExistsExceptIdAsync(string email, int id);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<User?> GetUserWithRelationsByIdAsync(int id);
        Task DeleteUserRelationsAsync(User user);
        Task DeleteUserAsync(User user);

        // Student Admin methods
        Task<List<User>> GetStudentsWithSubmissionsAndClassesAsync();
        Task<List<ClassExercise>> GetClassExercisesWithDeadlinesAsync(List<int> classIds);
        Task<User?> GetStudentWithSubmissionsAndScoresAsync(int studentId);
        Task<List<Submission>> GetSubmissionsWithScoresAsync(int studentId, int exerciseId);
        Task<Submission?> GetSubmissionWithExerciseAndScoresAsync(int submissionId);
    }
}
