using Microsoft.IdentityModel.Tokens;
using SpeakingBoost.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpeakingBoost.Services
{
    /// <summary>
    /// Triển khai tạo JWT token — tương đương HttpContext.SignInAsync() bên MVC
    /// Nhưng ở đây trả về chuỗi token thay vì set Cookie
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            // ✅ Các claims giống hệt MVC cũ — chỉ khác chỗ lưu (JWT thay vì Cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,           user.FullName),
                new Claim(ClaimTypes.Email,          user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Email),
                new Claim("StudentId",               user.UserId.ToString()),   // ← dùng User.FindFirst("StudentId") như MVC
                new Claim(ClaimTypes.Role,           user.Role.Trim().ToLower())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "180");

            var token = new JwtSecurityToken(
                issuer:   _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims:   claims,
                expires:  DateTime.Now.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
