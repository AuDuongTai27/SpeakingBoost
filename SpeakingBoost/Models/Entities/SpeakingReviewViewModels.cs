using SpeakingBoost.Models.Entities;
using System.Text.Json.Serialization;

namespace SpeakingBoost.Models.Entities
{
    public class AttemptHistoryItemVM
    {
        public int SubmissionId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? Overall { get; set; }
        public ProcessingStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AttemptDetailVM
    {
        public Submission Submission { get; set; }
        public Score Score { get; set; }
        public FeedbackRoot? FeedbackJson { get; set; }
    }

    // ===== JSON STRUCT cho AI Feedback =====

    public class FeedbackRoot
    {
        [JsonPropertyName("fluency_coherence")]
        public FeedbackCategory? FluencyCoherence { get; set; }

        [JsonPropertyName("lexical_resource")]
        public FeedbackCategory? LexicalResource { get; set; }

        [JsonPropertyName("grammar")]
        public FeedbackCategory? Grammar { get; set; }

        [JsonPropertyName("suggestion_answer")]
        public string? SuggestionAnswer { get; set; }
    }

    public class FeedbackCategory
    {
        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("issues")]
        public List<FeedbackIssue>? Issues { get; set; }

        [JsonPropertyName("issue_count")]
        public int IssueCount { get; set; }
    }

    public class FeedbackIssue
    {
        [JsonPropertyName("wrong")]
        public string? Wrong { get; set; }

        [JsonPropertyName("right")]
        public string? Right { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
