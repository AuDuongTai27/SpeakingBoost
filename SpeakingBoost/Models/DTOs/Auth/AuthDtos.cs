using System.ComponentModel.DataAnnotations;

namespace SpeakingBoost.Models.DTOs.Auth
{
    /// <summary>
    /// Body gửi lên khi đăng nhập — thay thế LoginRequest cũ trong LoginController MVC
    /// POST /api/auth/login
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dữ liệu trả về sau khi đăng nhập thành công
    /// </summary>
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// URL chuyển trang — giống redirectUrl bên MVC cũ
        /// </summary>
        public string RedirectUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Body gửi lên khi quên mật khẩu
    /// POST /api/auth/forgot-password
    /// </summary>
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Body gửi lên khi cập nhật profile cá nhân
    /// PUT /api/profile
    /// </summary>
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên quá dài")]
        public string FullName { get; set; } = string.Empty;

        public string? Password { get; set; }
    }
}
