using System.ComponentModel.DataAnnotations;

namespace SpeakingBoost.Models.DTOs.Admin
{
    // ============================================================
    // DTO dùng để trả về thông tin User trong response
    // KHÔNG trả entity User trực tiếp (chứa PasswordHash nhạy cảm)
    // ============================================================
    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // ============================================================
    // DTO nhận dữ liệu khi tạo User mới (POST)
    // Tương đương UserCreateViewModel bên MVC cũ
    // ============================================================
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public string Role { get; set; } = string.Empty; // admin | user

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;
    }

    // ============================================================
    // DTO nhận dữ liệu khi sửa User (PUT)
    // Không có Password — đổi mật khẩu dùng API riêng
    // ============================================================
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò là bắt buộc")]
        public string Role { get; set; } = string.Empty;
    }
}
