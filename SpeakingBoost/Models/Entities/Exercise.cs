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

        public string? SampleAnswer { get; set; }

        public int? TopicId { get; set; }

        public int MaxAttempts { get; set; } = 3;

        // --- Navigation Properties ---
        [ForeignKey("TopicId")]
        public virtual VocabularyTopic? VocabularyTopic { get; set; }

        public virtual ICollection<Submission>? Submissions { get; set; }
        public virtual ICollection<ClassExercise> ClassExercises { get; set; } = new List<ClassExercise>();
    }
}