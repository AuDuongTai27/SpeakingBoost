using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("VocabularyTopics")]
    public class VocabularyTopic
    {
        [Key]
        public int TopicId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        // --- Navigation Properties ---
        // Một Topic có thể có nhiều Exercises
        public virtual ICollection<Exercise>? Exercises { get; set; }
    }
}