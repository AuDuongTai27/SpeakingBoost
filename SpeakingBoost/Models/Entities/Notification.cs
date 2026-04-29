using System; // ⭐ Thêm
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpeakingBoost.Models.Entities
{
    [Table("Notifications")] // ⭐ Thêm
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; } // Giữ FK property

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ⭐ Sửa: Đặt [ForeignKey] đúng (trỏ đến "UserId")
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}