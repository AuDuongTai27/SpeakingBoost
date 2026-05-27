using SpeakingBoost.Models.DTOs.Admin;

namespace SpeakingBoost.Services.Interfaces.Admin
{
    public interface IExerciseService
    {
        Task<List<TopicDto>> GetAllTopicsAsync();
        Task<TopicDto> CreateTopicAsync(CreateTopicDto dto);
        Task UpdateTopicAsync(int id, CreateTopicDto dto);
        Task DeleteTopicAsync(int id);
        Task<TopicDetailsDto> GetTopicDetailsAsync(int id);
        Task<ExerciseDto> AddExerciseAsync(int topicId, CreateExerciseDto dto);
        Task<ExerciseDto> GetExerciseAsync(int id);
        Task UpdateExerciseAsync(int id, UpdateExerciseDto dto);
        Task DeleteExerciseAsync(int id);
        Task<int> ImportFromExcelAsync(int topicId, Stream excelStream);
    }
}
