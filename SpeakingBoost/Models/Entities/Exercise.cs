using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace SpeakingBoost.Models.Entities
{
    [Table("Exercises")]
    public class Exercise
    {
        [Key]
        public int ExerciseId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(10)]
        public string Type { get; set; } // "Part1", "Part2", "Part3"

        [Required]
        public string Question { get; set; }

        public string? SampleAnswer { get; set; } // Cho phép null

        [Required]
        public int CreatedBy { get; set; } // Khóa ngoại đến Users

        public int? TopicId { get; set; } // Khóa ngoại đến VocabularyTopics (cho phép null)

        public int MaxAttempts { get; set; } = 3;
        //public DateTime? Deadline { get; set; } // ⭐ THÊM DÒNG NÀY

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Navigation Properties ---
        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; } // Người tạo (Teacher)

        [ForeignKey("TopicId")]
        public virtual VocabularyTopic? VocabularyTopic { get; set; } // Chủ đề liên quan

        // Một Exercise có thể có nhiều Submissions
        public virtual ICollection<Submission>? Submissions { get; set; }
        public virtual ICollection<ClassExercise> ClassExercises { get; set; } = new List<ClassExercise>();
    }
}