using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Student
{
    public interface IStudentSubmissionRepository
    {
        /// <summary>Lấy tất cả submission của student (kèm Exercise + Scores)</summary>
        Task<List<Submission>> GetAllByStudentAsync(int studentId);

        /// <summary>Lấy submission practice của student cho 1 exercise (ClassExerciseId == null)</summary>
        Task<List<Submission>> GetPracticeHistoryAsync(int studentId, int exerciseId);

        /// <summary>Lấy submission deadline của student cho 1 classExercise</summary>
        Task<List<Submission>> GetDeadlineHistoryAsync(int studentId, int classExerciseId);

        /// <summary>Lấy chi tiết 1 submission theo id (phải thuộc về student)</summary>
        Task<Submission?> GetDetailAsync(int submissionId, int studentId);

        /// <summary>Lấy trạng thái + scores của 1 submission</summary>
        Task<Submission?> GetStatusAsync(int submissionId, int studentId);
    }
}
