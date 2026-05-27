using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Admin
{
    public interface IDeadlineRepository
    {
        Task<List<ClassExercise>> GetActiveDeadlinesAsync();
        Task<List<SchoolClass>> GetClassesSortedAsync();
        Task<List<VocabularyTopic>> GetTopicsSortedAsync();
        Task<List<Exercise>> GetExercisesByTopicIdAsync(int topicId);
        Task<SchoolClass?> GetClassByIdAsync(int classId);
        Task<VocabularyTopic?> GetTopicByIdAsync(int topicId);
        Task<ClassExercise?> GetClassExerciseAsync(int classId, int exerciseId);
        Task AddClassExerciseAsync(ClassExercise classExercise);
        Task UpdateClassExerciseAsync(ClassExercise classExercise);
        Task<ClassExercise?> GetClassExerciseByIdAsync(int id);
        Task DeleteClassExerciseAsync(ClassExercise classExercise);
        Task<List<ClassExercise>> GetClassExercisesAsync(int classId, List<int> exerciseIds);
        Task DeleteClassExercisesRangeAsync(List<ClassExercise> assignments);
        Task<List<User>> GetStudentsByClassIdAsync(int classId);
    }
}
