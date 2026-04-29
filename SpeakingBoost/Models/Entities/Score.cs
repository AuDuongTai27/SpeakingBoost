using System; // ⭐ Thêm
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("Scores")] // ⭐ Thêm
    public class Score
    {
        [Key]
        public int ScoreId { get; set; }

        public int SubmissionId { get; set; } // Giữ FK property

        // Nên cho phép null để khớp với CSDL
        public double? Pronunciation { get; set; }
        public double? Grammar { get; set; }
        public double? LexicalResource { get; set; }
        public double? Coherence { get; set; }

        // ⭐ Sửa 1: Báo cho EF Core đây là cột CSDL tự tính
        public double? Overall { get; set; }

        public string? AiFeedback { get; set; } // AI feedback là file json dùng để đọc các lỗi cụ thể.

        // ⭐ Sửa 2: Thêm lại cột CreatedAt
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ⭐ Sửa 3: Đặt [ForeignKey] đúng (trỏ đến "SubmissionId")
        [ForeignKey("SubmissionId")]
        public virtual Submission? Submission { get; set; }
    }
}