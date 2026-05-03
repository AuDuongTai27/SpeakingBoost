using System.ComponentModel.DataAnnotations;

namespace SpeakingBoost.Models.DTOs.Admin
{
    // ================================================================
    // DTOs — ClassesController
    // ================================================================

    public class ClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class CreateClassDto
    {
        [Required(ErrorMessage = "Tên lớp là bắt buộc")]
        [MaxLength(100)]
        public string ClassName { get; set; } = string.Empty;
    }

    public class UpdateClassDto
    {
        [Required(ErrorMessage = "Tên lớp là bắt buộc")]
        [MaxLength(100)]
        public string ClassName { get; set; } = string.Empty;
    }

    public class AddStudentToClassDto
    {
        [Required]
        public int StudentId { get; set; }
    }

    public class AssignExerciseDto
    {
        [Required]
        public int ExerciseId { get; set; }
        public DateTime? Deadline { get; set; }
    }

    public class UpdateDeadlineInClassDto
    {
        public DateTime? Deadline { get; set; }
    }

    public class ClassDetailsDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public List<StudentInClassDto> Students { get; set; } = new();
        public List<AssignedExerciseDto> AssignedExercises { get; set; } = new();
    }

    public class StudentInClassDto
    {
        public int StudentClassId { get; set; }
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SubmissionCount { get; set; }
    }

    public class AssignedExerciseDto
    {
        public int ClassExerciseId { get; set; }
        public int ExerciseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? TopicName { get; set; }
        public DateTime? Deadline { get; set; }
        public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now;
    }

    public class StudentSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int SubmittedCount { get; set; }
        public int SubmittedLateCount { get; set; }
        public int MissingCount { get; set; }
    }

    public class StudentDetailsDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<SubmissionSummaryDto> Submissions { get; set; } = new();
        public List<string> ChartLabels { get; set; } = new();
        public List<double> ChartValues { get; set; } = new();
    }

    public class SubmissionSummaryDto
    {
        public int SubmissionId { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double? Overall { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AttemptHistoryAdminDto
    {
        public int SubmissionId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? Overall { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class AttemptDetailAdminDto
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ExerciseTitle { get; set; } = string.Empty;
        public string? AudioPath { get; set; }
        public string? Transcript { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? Overall { get; set; }
        public double? Pronunciation { get; set; }
        public double? Grammar { get; set; }
        public double? LexicalResource { get; set; }
        public double? Coherence { get; set; }
        public string? AiFeedback { get; set; }
    }

    // ================================================================
    // DTOs — TestsController (Exercise & Topic Management)
    // ================================================================

    public class TopicDto
    {
        public int TopicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ExerciseCount { get; set; }
    }

    public class CreateTopicDto
    {
        [Required(ErrorMessage = "Tên chủ đề là bắt buộc")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }
    }

    public class TopicDetailsDto
    {
        public int TopicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<ExerciseDto> Exercises { get; set; } = new();
    }

    public class ExerciseDto
    {
        public int ExerciseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string? SampleAnswer { get; set; }
        public int MaxAttempts { get; set; }
        public int? TopicId { get; set; }
        public string? TopicName { get; set; }
    }

    public class CreateExerciseDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại bài là bắt buộc (Part1/Part2/Part3)")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Câu hỏi là bắt buộc")]
        public string Question { get; set; } = string.Empty;

        public string? SampleAnswer { get; set; }

        [Range(1, 10)]
        public int MaxAttempts { get; set; } = 3;

        public int? TopicId { get; set; }
    }

    public class UpdateExerciseDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Question { get; set; } = string.Empty;

        public string? SampleAnswer { get; set; }

        [Range(1, 10)]
        public int MaxAttempts { get; set; } = 3;

        public int? TopicId { get; set; }
    }

    // ================================================================
    // DTOs — DeadlinesController
    // Deadline chỉ giao theo Topic (không có Custom mode)
    // ================================================================

    public class ActiveDeadlineDto
    {
        public int ClassExerciseId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        public string? TopicName { get; set; }
        public DateTime? Deadline { get; set; }
        public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateTime.Now;
    }

    /// <summary>
    /// Giao toàn bộ câu hỏi của 1 Topic cho 1 Lớp với cùng Deadline.
    /// Thay thế BulkAssignDeadlineDto cũ (đã bỏ Custom mode).
    /// </summary>
    public class AssignTopicDeadlineDto
    {
        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chủ đề")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ngày deadline")]
        public DateTime Deadline { get; set; }
    }

    // ================================================================
    // DTOs — AdminDashboardController
    // ================================================================

    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalExercises { get; set; }
        public double SubmissionRate { get; set; }
        public double AverageOverallScore { get; set; }
        public List<ChartDataPointDto> ProgressChartData { get; set; } = new();
        public List<ChartDataPointDto> SkillsChartData { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public List<ClassDto> ClassList { get; set; } = new();
    }

    public class ChartDataPointDto
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class RecentActivityDto
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ExerciseTitle { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public double? Overall { get; set; }
    }
}
