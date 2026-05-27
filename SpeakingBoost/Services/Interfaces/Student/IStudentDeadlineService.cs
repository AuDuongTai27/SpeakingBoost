using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;

namespace SpeakingBoost.Services.Interfaces.Student
{
    public interface IStudentDeadlineService
    {
        /// <summary>Lấy danh sách bài tập có deadline của student</summary>
        Task<BaseResponse<List<DeadlineExerciseDto>>> GetDeadlinesAsync(int studentId);

        /// <summary>Lấy chi tiết câu hỏi của 1 deadline (kiểm tra quyền truy cập)</summary>
        Task<BaseResponse<DeadlineQuestionDto>> GetDeadlineQuestionAsync(int classExerciseId, int studentId);

        /// <summary>Nộp audio cho bài tập deadline, đưa vào background queue</summary>
        Task<BaseResponse<SubmitAudioResponse>> SubmitAudioAsync(
            IFormFile audio,
            int exerciseId,
            int classExerciseId,
            int part,
            int studentId,
            IServiceProvider serviceProvider);
    }
}
