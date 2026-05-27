using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;

namespace SpeakingBoost.Services.Interfaces.Student
{
    public interface IStudentDashboardService
    {
        /// <summary>Lấy toàn bộ dữ liệu dashboard của student (assignments, thống kê, biểu đồ)</summary>
        Task<BaseResponse<StudentDashboardDto>> GetDashboardAsync(int studentId);
    }
}
