using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("Classes")]
    public class SchoolClass
    {
        [Key]
        public int ClassId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ClassName { get; set; }

        [Required] // Cột này là NOT NULL trong SQL của bạn
        public int TeacherId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- Navigation Properties ---

        [ForeignKey("TeacherId")]
        public virtual User? Teacher { get; set; }

        // Danh sách học sinh trong lớp (qua bảng StudentClasses)
        public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
        public virtual ICollection<ClassExercise> ClassExercises { get; set; } = new List<ClassExercise>();
    }
}