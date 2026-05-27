using SpeakingBoost.Models.DTOs.Admin;

namespace SpeakingBoost.Services.Interfaces.Admin
{
    public interface IDeadlineService
    {
        Task<object> GetActiveDeadlinesDataAsync();
        Task<string> AssignTopicDeadlineAsync(AssignTopicDeadlineDto dto);
        Task DeleteDeadlineAsync(int id);
        Task DeleteTopicDeadlineFromClassAsync(int topicId, int classId);
    }
}
