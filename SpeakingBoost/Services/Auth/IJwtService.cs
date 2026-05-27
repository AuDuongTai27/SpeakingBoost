using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Services.Auth
{
    /// <summary>
    /// Interface tạo và xác thực JWT token
    /// </summary>
    public interface IJwtService
    {
        /// <summary>Tạo JWT token từ thông tin User</summary>
        string GenerateToken(User user);
    }
}
