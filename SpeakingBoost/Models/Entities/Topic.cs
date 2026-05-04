using System.ComponentModel.DataAnnotations;

namespace SpeakingBoost.Models.Entities
{
    // Topic dùng cho Practice view (IELTS Speaking forecast topics)
    public class Topic
    {
        public int TopicId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [StringLength(100)]
        public string ForecastLabel { get; set; }

        public DateTime ForecastDate { get; set; }

        public int Part { get; set; } // IELTS Speaking Part 1 / 2 / 3

        public ICollection<Exercise>? Exercises { get; set; }
    }
}
