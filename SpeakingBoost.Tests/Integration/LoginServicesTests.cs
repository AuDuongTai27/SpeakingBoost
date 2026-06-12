using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Services.Implementations.Auth;
using SpeakingBoost.Tests._Helpers;
using Xunit;

namespace SpeakingBoost.Tests.Integration
{
    /// <summary>
    /// Integration Tests cho LoginServices — dùng EF Core InMemory Database
    /// Xếp vào Integration Tests vì service phụ thuộc trực tiếp vào ApplicationDbContext
    /// (không qua Interface/Repository), buộc phải chạy qua EF Core engine thật (in-memory).
    /// AAA Pattern: Arrange (chuẩn bị) → Act (thực thi) → Assert (kiểm tra)
    /// </summary>
    public class LoginServicesTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly LoginServices _service;

        public LoginServicesTests()
        {
            // Mỗi test class dùng 1 tên DB riêng để tránh dữ liệu bị lẫn
            _context = InMemoryDbHelper.CreateContext($"LoginDb_{Guid.NewGuid()}");
            _service = new LoginServices(_context);

            // Seed dữ liệu test chung
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Hash password "Password123" trước để insert đúng format
            var hashedPassword = _service.HashPassword("Password123");

            _context.Users.AddRange(
                new User
                {
                    UserId       = 1,
                    FullName     = "Nguyễn Văn A",
                    Email        = "student@test.com",
                    PasswordHash = hashedPassword,
                    Role         = "user"
                },
                new User
                {
                    UserId       = 2,
                    FullName     = "Admin Test",
                    Email        = "admin@test.com",
                    PasswordHash = _service.HashPassword("AdminPass"),
                    Role         = "admin"
                }
            );
            _context.SaveChanges();
        }

        // ─────────────────────────────────────────────────────
        // TC-L01: Đăng nhập đúng email và password → trả về User
        // ─────────────────────────────────────────────────────
        [Fact]
        public void Login_WithCorrectCredentials_ReturnsUser()
        {
            // Act
            var result = _service.Login("student@test.com", "Password123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("student@test.com", result.Email);
        }

        // ─────────────────────────────────────────────────────
        // TC-L02: Sai password → trả về null
        // ─────────────────────────────────────────────────────
        [Fact]
        public void Login_WithWrongPassword_ReturnsNull()
        {
            // Act
            var result = _service.Login("student@test.com", "WrongPassword");

            // Assert
            Assert.Null(result);
        }

        // ─────────────────────────────────────────────────────
        // TC-L03: Email không tồn tại → trả về null
        // ─────────────────────────────────────────────────────
        [Fact]
        public void Login_WithNonexistentEmail_ReturnsNull()
        {
            // Act
            var result = _service.Login("notexist@test.com", "Password123");

            // Assert
            Assert.Null(result);
        }

        // ─────────────────────────────────────────────────────
        // TC-L04: Email có khoảng trắng thừa → vẫn đăng nhập được (tự trim)
        // ─────────────────────────────────────────────────────
        [Fact]
        public void Login_WithEmailHavingWhitespace_TrimsAndReturnsUser()
        {
            // Act
            var result = _service.Login("  student@test.com  ", "Password123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("student@test.com", result.Email);
        }

        // ─────────────────────────────────────────────────────
        // TC-L05: HashPassword gọi 2 lần với cùng input → cho cùng kết quả
        // ─────────────────────────────────────────────────────
        [Fact]
        public void HashPassword_SameInput_ReturnsSameHash()
        {
            // Act
            var hash1 = _service.HashPassword("MyPassword");
            var hash2 = _service.HashPassword("MyPassword");

            // Assert
            Assert.Equal(hash1, hash2);
        }

        // ─────────────────────────────────────────────────────
        // TC-L06: HashPassword với password khác nhau → hash khác nhau
        // ─────────────────────────────────────────────────────
        [Fact]
        public void HashPassword_DifferentPasswords_ReturnDifferentHashes()
        {
            // Act
            var hash1 = _service.HashPassword("Password1");
            var hash2 = _service.HashPassword("Password2");

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        // ─────────────────────────────────────────────────────
        // TC-L07: UpdatePassword với userId tồn tại → trả về true
        // ─────────────────────────────────────────────────────
        [Fact]
        public void UpdatePassword_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var newHash = _service.HashPassword("NewPassword456");

            // Act
            var result = _service.UpdatePassword(1, newHash);

            // Assert
            Assert.True(result);
            // Verify password was actually updated in the in-memory DB
            var updatedUser = _context.Users.Find(1);
            Assert.Equal(newHash, updatedUser!.PasswordHash);
        }

        // ─────────────────────────────────────────────────────
        // TC-L08: UpdatePassword với userId không tồn tại → trả về false
        // ─────────────────────────────────────────────────────
        [Fact]
        public void UpdatePassword_NonexistentUser_ReturnsFalse()
        {
            // Act
            var result = _service.UpdatePassword(9999, "somehash");

            // Assert
            Assert.False(result);
        }

        // ─────────────────────────────────────────────────────
        // TC-L09: GetUserByEmail trả về đúng user theo email
        // ─────────────────────────────────────────────────────
        [Fact]
        public void GetUserByEmail_ExistingEmail_ReturnsUser()
        {
            // Act
            var result = _service.GetUserByEmail("admin@test.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("admin", result.Role);
            Assert.Equal("Admin Test", result.FullName);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
