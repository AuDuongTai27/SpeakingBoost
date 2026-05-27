using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Repositories.Interfaces.Student
{
    public interface IStudentDeadlineRepository
    {
        /// <summary>Lấy danh sách ClassId mà student đang thuộc vào</summary>
        Task<List<int>> GetClassIdsByStudentAsync(int studentId);

        /// <summary>Lấy ClassExercise có deadline của các lớp student thuộc vào</summary>
        Task<List<ClassExercise>> GetDeadlinesByClassIdsAsync(List<int> classIds);

        /// <summary>Lấy submission có ClassExerciseId (deadline submissions) của student</summary>
        Task<List<Submission>> GetDeadlineSubmissionsAsync(int studentId);

        /// <summary>Lấy ClassExercise cụ thể kèm Exercise + SchoolClass</summary>
        Task<ClassExercise?> GetClassExerciseWithDetailsAsync(int classExerciseId);

        /// <summary>Kiểm tra student có trong lớp không</summary>
        Task<bool> IsStudentInClassAsync(int studentId, int classId);

        /// <summary>Đếm số lần đã nộp cho 1 deadline (classExerciseId)</summary>
        Task<int> CountAttemptsByClassExerciseAsync(int studentId, int classExerciseId);

        /// <summary>Lấy submission mới nhất của student cho 1 deadline</summary>
        Task<Submission?> GetLatestDeadlineSubmissionAsync(int studentId, int classExerciseId);

        /// <summary>Thêm submission mới</summary>
        Task<Submission> AddSubmissionAsync(Submission submission);
    }
}
