using SpeakingBoost.Models.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SpeakingBoost.Models.EF
{
    /// <summary>
    /// DbSeeder — tự động tạo tài khoản mẫu nếu chưa tồn tại trong DB.
    /// Chỉ chạy khi môi trường là Development.
    ///
    /// Tài khoản seed:
    ///   student1@example.com  / Password123  (role: student)
    ///   teacher1@example.com  / Password123  (role: teacher)
    ///   admin1@example.com    / Password123  (role: superadmin)
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                var seedUsers = new[]
                {
                    new { FullName = "Student Test",    Email = "student1@example.com", Role = "student",    Password = "Password123" },
                    new { FullName = "Teacher Test",    Email = "teacher1@example.com", Role = "teacher",    Password = "Password123" },
                    new { FullName = "SuperAdmin Test", Email = "admin1@example.com",   Role = "superadmin", Password = "Password123" },
                };

                bool anyAdded = false;

                foreach (var s in seedUsers)
                {
                    // Chỉ insert nếu email chưa tồn tại → không ghi đè data thật
                    if (!context.Users.Any(u => u.Email == s.Email))
                    {
                        context.Users.Add(new User
                        {
                            FullName     = s.FullName,
                            Email        = s.Email,
                            Role         = s.Role,
                            PasswordHash = HashPassword(s.Password),
                            CreatedAt    = DateTime.Now
                        });

                        logger.LogInformation("[DbSeeder] Thêm tài khoản seed: {Email} ({Role})", s.Email, s.Role);
                        anyAdded = true;
                    }
                    else
                    {
                        logger.LogInformation("[DbSeeder] Đã tồn tại, bỏ qua: {Email}", s.Email);
                    }
                }

                if (anyAdded)
                    context.SaveChanges();

                logger.LogInformation("[DbSeeder] Seed hoàn tất.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DbSeeder] Lỗi khi seed dữ liệu.");
            }
        }

        // Phải dùng cùng thuật toán SHA-256 giống LoginServices.HashPassword()
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
