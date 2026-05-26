using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Student
{
    public interface IStudentDashboardRepository
    {
        /// <summary>Lấy danh sách ClassId mà student đang thuộc vào</summary>
        Task<List<int>> GetClassIdsByStudentAsync(int studentId);

        /// <summary>Lấy tất cả ClassExercise (bài tập được giao) của các lớp đó</summary>
        Task<List<ClassExercise>> GetAssignedExercisesAsync(List<int> classIds);

        /// <summary>Lấy tất cả submission (kèm Scores) của student</summary>
        Task<List<Submission>> GetStudentSubmissionsWithScoresAsync(int studentId);
    }
}
