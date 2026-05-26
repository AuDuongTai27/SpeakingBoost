using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Admin
{
    public interface IExerciseRepository
    {
        Task<List<VocabularyTopic>> GetAllTopicsAsync();
        Task<VocabularyTopic?> GetTopicByIdAsync(int id);
        Task<bool> TopicNameExistsAsync(string name);
        Task<bool> TopicNameExistsExceptIdAsync(string name, int id);
        Task AddTopicAsync(VocabularyTopic topic);
        Task UpdateTopicAsync(VocabularyTopic topic);
        Task<VocabularyTopic?> GetTopicWithExercisesAsync(int id);
        Task<bool> HasSubmissionsForExercisesAsync(List<int> exerciseIds);
        Task DeleteExercisesRangeAsync(List<Exercise> exercises);
        Task DeleteTopicAsync(VocabularyTopic topic);
        Task AddExerciseAsync(Exercise exercise);
        Task<Exercise?> GetExerciseByIdAsync(int id);
        Task<Exercise?> GetExerciseWithTopicByIdAsync(int id);
        Task UpdateExerciseAsync(Exercise exercise);
        Task<Exercise?> GetExerciseWithSubmissionsByIdAsync(int id);
        Task DeleteSubmissionsRangeAsync(List<Submission> submissions);
        Task DeleteExerciseAsync(Exercise exercise);
        Task AddExercisesRangeAsync(List<Exercise> exercises);
    }
}
