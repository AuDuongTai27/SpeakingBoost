using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Student
{
    public interface IPracticeRepository
    {
        /// <summary>Lấy tất cả topics kèm số câu hỏi (lọc theo part nếu có)</summary>
        Task<List<(int TopicId, string Name, string? Description, int QuestionCount)>> GetTopicsWithCountAsync(int part);

        /// <summary>Lấy thông tin topic (id + name)</summary>
        Task<(int TopicId, string Name)?> GetTopicHeaderAsync(int topicId);

        /// <summary>Lấy danh sách câu hỏi trong topic kèm số lần student đã nộp</summary>
        Task<List<(Exercise Exercise, int AttemptUsed)>> GetTopicQuestionsWithAttemptsAsync(int topicId, int part, int studentId);

        /// <summary>Thêm submission mới (practice)</summary>
        Task<Submission> AddSubmissionAsync(Submission submission);

        /// <summary>Cập nhật submission (khi queue bận)</summary>
        Task UpdateSubmissionAsync(Submission submission);
    }
}
