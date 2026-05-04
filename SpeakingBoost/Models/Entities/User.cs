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
        public string Role { get; set; }  // admin | user

        // 🔗 Quan hệ
        [InverseProperty("Student")]
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

        [InverseProperty("Student")]
        public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}