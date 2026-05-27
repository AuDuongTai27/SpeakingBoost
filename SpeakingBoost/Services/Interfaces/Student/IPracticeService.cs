using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;

namespace SpeakingBoost.Services.Interfaces.Student
{
    public interface IPracticeService
    {
        /// <summary>Lấy danh sách topic luyện tập (lọc theo part)</summary>
        Task<BaseResponse<List<PracticeTopicDto>>> GetTopicsAsync(int part);

        /// <summary>Lấy danh sách câu hỏi trong 1 topic (kèm số lần đã nộp)</summary>
        Task<BaseResponse<List<PracticeQuestionDto>>> GetTopicQuestionsAsync(int topicId, int part, int studentId);

        /// <summary>Nộp audio practice, đưa vào background queue</summary>
        Task<BaseResponse<SubmitAudioResponse>> SubmitAudioAsync(
            IFormFile audio,
            int exerciseId,
            int part,
            int studentId,
            IServiceProvider serviceProvider);
    }
}
