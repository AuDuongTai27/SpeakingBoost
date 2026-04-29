namespace SpeakingBoost.Models.Entities
{
    public class PracticeTopicViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ForecastLabel { get; set; }
        public string ForecastDate { get; set; }
        public int Part { get; set; }
        public int QuestionCount { get; set; }
        public List<string> Questions { get; set; }
        public List<int> Parts { get; set; }
        public List<int> ExerciseIds { get; set; }
        public List<int> MaxAttemptsList { get; set; } = new();
        public List<int> AttemptUsedList { get; set; } = new();
    }
}
