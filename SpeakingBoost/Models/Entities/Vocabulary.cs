using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("Vocabulary")]
    public class Vocabulary
    {
        [Key]
        public int VocabId { get; set; }

        public int? StudentId { get; set; } // Khóa ngoại đến Users (cho phép null)

        public int? TopicId { get; set; } // Khóa ngoại đến VocabularyTopics (cho phép null)

        [Required]
        [MaxLength(50)]
        public string Word { get; set; }

        [MaxLength(255)]
        public string? Meaning { get; set; }

        [MaxLength(255)]
        public string? Example { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Navigation Properties ---
        [ForeignKey("StudentId")]
        public virtual User? Student { get; set; } // User sở hữu từ này (nếu có)

        [ForeignKey("TopicId")]
        public virtual VocabularyTopic? VocabularyTopic { get; set; } // Chủ đề liên quan (nếu có)
    }
}