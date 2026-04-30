namespace SpeakingBoost.Models.DTOs.Student
{
    public class StudentDashboardDto
    {
        public List<StudentAssignmentDto> UpcomingAssignments { get; set; } = new();
        public int PendingAssignmentsCount { get; set; }
        public int OverdueAssignmentsCount { get; set; }
        public int CompletedExercisesCount { get; set; }
        public double AverageScore { get; set; }
        public List<string> ChartLabels { get; set; } = new();
        public List<double> ChartData { get; set; } = new();
    }

    public class StudentAssignmentDto
    {
        public int ExerciseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public double? Score { get; set; }
        public int TopicId { get; set; }
        public int Part { get; set; }
        public string Status { get; set; } = string.Empty; // Submitted | Overdue | Pending
    }
}
