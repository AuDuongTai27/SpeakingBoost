namespace SpeakingBoost.Models.DTOs.Student
{
    public class AttemptHistoryItemDto
    {
        public int SubmissionId { get; set; }
        public int? ClassExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? Overall { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class AttemptDetailDto
    {
        public int SubmissionId { get; set; }
        public int? ClassExerciseId { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseTitle { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string? SampleAnswer { get; set; }
        public string AudioPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int AttemptNumber { get; set; }
        public double? OverallScore { get; set; }
        public double? Pronunciation { get; set; }
        public double? Grammar { get; set; }
        public double? LexicalResource { get; set; }
        public double? Coherence { get; set; }
        public string Transcript { get; set; } = string.Empty;
        public string? AiFeedback { get; set; }
        public object? FeedbackJson { get; set; }
        public string? ErrorMessage { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
