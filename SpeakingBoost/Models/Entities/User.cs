using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(64)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(10)]
        public string Role { get; set; }  // Student | Teacher

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 🔗 Quan hệ
        [InverseProperty("CreatedByUser")]
        public virtual ICollection<Exercise> CreatedExercises { get; set; } = new List<Exercise>();

        [InverseProperty("Teacher")]
        public virtual ICollection<SchoolClass> TaughtClasses { get; set; } = new List<SchoolClass>();

        // ✅ SỬA: Thêm ForeignKey để EF biết dùng StudentId, không phải UserId
        [InverseProperty("Student")]
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

        public virtual ICollection<Vocabulary> Vocabularies { get; set; } = new List<Vocabulary>();

        [InverseProperty("Student")]  // ✅ THÊM: Chỉ rõ navigation property
        public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}