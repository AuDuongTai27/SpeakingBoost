using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("StudentClasses")]
    public class StudentClass
    {
        [Key]
        public int StudentClassId { get; set; }

        [Required]
        public int StudentId { get; set; } // <-- Phải là StudentId

        [Required]
        public int ClassId { get; set; }

        [ForeignKey("StudentId")] // <-- Phải trỏ đến "StudentId"
        public virtual User Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual SchoolClass SchoolClass { get; set; }
    }
}