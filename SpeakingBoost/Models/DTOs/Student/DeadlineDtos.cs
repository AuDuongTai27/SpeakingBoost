namespace SpeakingBoost.Models.DTOs.Student
{
    public class DeadlineExerciseDto
    {
        public int ClassExerciseId { get; set; }
        public int ExerciseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double? Score { get; set; }
        public int SubmissionId { get; set; }
    }

    public class DeadlineQuestionDto
    {
        public int ClassExerciseId { get; set; }
        public int ExerciseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public int Part { get; set; }
        public DateTime? Deadline { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int MaxAttempts { get; set; }
        public int AttemptUsed { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
