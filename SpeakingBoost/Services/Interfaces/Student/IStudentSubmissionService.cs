using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;

namespace SpeakingBoost.Services.Interfaces.Student
{
    public interface IStudentSubmissionService
    {
        /// <summary>Lấy toàn bộ lịch sử nộp bài của student</summary>
        Task<BaseResponse<List<AttemptHistoryItemDto>>> GetAllHistoryAsync(int studentId);

        /// <summary>Lấy lịch sử practice của 1 exercise (ClassExerciseId == null)</summary>
        Task<BaseResponse<List<AttemptHistoryItemDto>>> GetPracticeHistoryAsync(int studentId, int exerciseId);

        /// <summary>Lấy lịch sử deadline của 1 classExercise</summary>
        Task<BaseResponse<List<AttemptHistoryItemDto>>> GetDeadlineHistoryAsync(int studentId, int classExerciseId);

        /// <summary>Lấy chi tiết 1 lần nộp bài (kèm scores, feedback)</summary>
        Task<BaseResponse<AttemptDetailDto>> GetAttemptDetailAsync(int submissionId, int studentId);

        /// <summary>Lấy trạng thái xử lý của 1 submission (polling)</summary>
        Task<BaseResponse<object>> GetStatusAsync(int submissionId, int studentId);
    }
}
