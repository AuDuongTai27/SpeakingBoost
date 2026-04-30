namespace SpeakingBoost.Models.DTOs.Student
{
    public class VocabularyTopicDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class WordDto
    {
        public string Word { get; set; } = string.Empty;
        public string? Meaning { get; set; }
        public string? Example { get; set; }
    }

    public class VocabularyDetailsDto
    {
        public string TopicName { get; set; } = string.Empty;
        public List<WordDto> Words { get; set; } = new();
    }
}
