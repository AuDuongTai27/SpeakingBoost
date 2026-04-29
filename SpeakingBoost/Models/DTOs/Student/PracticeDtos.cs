namespace SpeakingBoost.Models.DTOs.Student
{
    /// <summary>
    /// DTO danh sách topics luyện tập — thay thế PracticeTopicViewModel bên MVC
    /// </summary>
    public class PracticeTopicDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ForecastLabel { get; set; } = string.Empty;
        public string ForecastDate { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    /// <summary>
    /// DTO chi tiết câu hỏi trong 1 topic (kèm thông tin số lần đã nộp)
    /// </summary>
    public class PracticeQuestionDto
    {
        public int ExerciseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Part1 | Part2 | Part3
        public int MaxAttempts { get; set; }
        public int AttemptUsed { get; set; }
        public bool CanSubmit => AttemptUsed < MaxAttempts;
    }

    /// <summary>
    /// DTO trả về sau khi submit audio thành công (trạng thái Pending)
    /// </summary>
    public class SubmitAudioResponse
    {
        public int SubmissionId { get; set; }
        public string Status { get; set; } = "Pending";
        public string Message { get; set; } = "Đang xử lý trong nền, vui lòng chờ...";
    }
}
