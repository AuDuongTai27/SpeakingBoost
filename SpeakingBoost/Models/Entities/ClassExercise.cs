using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("ClassExercises")]
    public class ClassExercise
    {
        [Key]
        public int ClassExerciseId { get; set; }
        public int ClassId { get; set; }
        public int ExerciseId { get; set; }
        public DateTime? Deadline { get; set; }

        [ForeignKey("ClassId")]
        public virtual SchoolClass SchoolClass { get; set; }
        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; }
    }
}