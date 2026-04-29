using SpeakingBoost.Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("Submissions")]
    public class Submission
    {
        [Key]
        public int SubmissionId { get; set; }

        // FK tới User (Student)
        [Required]
        public int StudentId { get; set; }

        // ✅ SỬA: Bỏ [InverseProperty], chỉ giữ [ForeignKey]
        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }

        // FK tới Exercise
        [Required]
        public int ExerciseId { get; set; }

        [ForeignKey("ExerciseId")]
        public virtual Exercise Exercise { get; set; }

        [MaxLength(255)]
        public string? AudioPath { get; set; }

        public string? Transcript { get; set; }

        public int AttemptNumber { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ Một-Nhiều với Scores
        public virtual ICollection<Score> Scores { get; set; } = new List<Score>();

        // 2. Thêm cột trạng thái xử lý
        public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

        // 3. Thêm cột lưu lỗi (nếu có)
        public string? ErrorMessage { get; set; }

        
    }
    public enum ProcessingStatus
    {
        Pending,    // Đang chờ
        Processing, // Đang chấm
        Completed,  // Đã xong
        Failed      // Lỗi
    }
}
