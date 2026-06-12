using Moq;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Implementations.Admin;
using SpeakingBoost.Services.Interfaces.Auth;
using Xunit;

namespace SpeakingBoost.Tests.Services
{
    /// <summary>
    /// Unit Tests cho UserService — dùng Moq để mock IUserRepository và ILoginServices
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository>   _mockUserRepo;
        private readonly Mock<ILoginServices>    _mockLoginSvc;
        private readonly UserService             _service;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockLoginSvc = new Mock<ILoginServices>();
            _service      = new UserService(_mockUserRepo.Object, _mockLoginSvc.Object);
        }

        // ─────────────────────────────────────────────────────
        // TC-U01: Tạo user mới thành công → trả về UserDto với email lowercase
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task CreateUserAsync_ValidDto_ReturnsUserDto()
        {
            // Arrange
            var dto = new CreateUserDto
            {
                FullName = "Trần Thị B",
                Email    = "STUDENT@EXAMPLE.COM",   // uppercase → phải normalize
                Password = "secret123"
            };

            _mockUserRepo.Setup(r => r.EmailExistsAsync("student@example.com")).ReturnsAsync(false);
            _mockLoginSvc.Setup(s => s.HashPassword("secret123")).Returns("hashed_value");
            _mockUserRepo.Setup(r => r.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateUserAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("student@example.com", result.Email); // đã normalize
            Assert.Equal("user", result.Role);
            Assert.Equal("Trần Thị B", result.FullName);
        }

        // ─────────────────────────────────────────────────────
        // TC-U02: Tạo user với email đã tồn tại → ném InvalidOperationException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new CreateUserDto { FullName = "Test", Email = "taken@test.com", Password = "123456" };
            _mockUserRepo.Setup(r => r.EmailExistsAsync("taken@test.com")).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateUserAsync(dto));
            Assert.Contains("đã được sử dụng", ex.Message);
        }

        // ─────────────────────────────────────────────────────
        // TC-U03: GetUserById trả về user hợp lệ (role = "user")
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetUserByIdAsync_ValidStudentId_ReturnsUserDto()
        {
            // Arrange
            var user = new User { UserId = 5, FullName = "Học Sinh", Email = "hs@test.com", Role = "user" };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(5)).ReturnsAsync(user);

            // Act
            var result = await _service.GetUserByIdAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.UserId);
            Assert.Equal("user", result.Role);
        }

        // ─────────────────────────────────────────────────────
        // TC-U04: GetUserById với role admin → ném KeyNotFoundException
        // (Admin không phải học sinh, không tìm được theo API học sinh)
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetUserByIdAsync_AdminRole_ThrowsKeyNotFoundException()
        {
            // Arrange
            var adminUser = new User { UserId = 1, FullName = "Admin", Email = "admin@test.com", Role = "admin" };
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(adminUser);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetUserByIdAsync(1));
        }

        // ─────────────────────────────────────────────────────
        // TC-U05: GetUserById với id không tồn tại → ném KeyNotFoundException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetUserByIdAsync_NonexistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetUserByIdAsync(999)).ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetUserByIdAsync(999));
        }

        // ─────────────────────────────────────────────────────
        // TC-U06: UpdateUser email đã dùng bởi người khác → ném InvalidOperationException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateUserAsync_EmailTakenByOther_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = new User { UserId = 2, Email = "original@test.com", Role = "user" };
            var dto  = new UpdateUserDto { FullName = "New Name", Email = "TAKEN@TEST.COM" };

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(2)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.EmailExistsExceptIdAsync("taken@test.com", 2)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateUserAsync(2, dto));
        }

        // ─────────────────────────────────────────────────────
        // TC-U07: UpdateUser thành công → email được normalize lowercase
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateUserAsync_ValidData_NormalizesEmail()
        {
            // Arrange
            var user = new User { UserId = 3, Email = "old@test.com", FullName = "Old Name", Role = "user" };
            var dto  = new UpdateUserDto { FullName = "New Name", Email = "  NEW@TEST.COM  " };

            _mockUserRepo.Setup(r => r.GetUserByIdAsync(3)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.EmailExistsExceptIdAsync("new@test.com", 3)).ReturnsAsync(false);
            _mockUserRepo.Setup(r => r.UpdateUserAsync(user)).Returns(Task.CompletedTask);

            // Act
            await _service.UpdateUserAsync(3, dto);

            // Assert: email đã được trim và lowercase
            Assert.Equal("new@test.com", user.Email);
            Assert.Equal("New Name", user.FullName);
        }

        // ─────────────────────────────────────────────────────
        // TC-U08: DeleteUser không tồn tại → ném KeyNotFoundException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteUserAsync_NonexistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserRepo.Setup(r => r.GetUserWithRelationsByIdAsync(999)).ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteUserAsync(999));
        }

        // ─────────────────────────────────────────────────────
        // TC-U09: DeleteUser hợp lệ → xóa relations trước, rồi xóa user
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteUserAsync_ValidId_DeletesRelationsBeforeUser()
        {
            // Arrange
            var user = new User { UserId = 4, Role = "user", FullName = "Student To Delete", Email = "del@test.com" };
            _mockUserRepo.Setup(r => r.GetUserWithRelationsByIdAsync(4)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.DeleteUserRelationsAsync(user)).Returns(Task.CompletedTask);
            _mockUserRepo.Setup(r => r.DeleteUserAsync(user)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteUserAsync(4);

            // Assert: phải gọi DeleteUserRelationsAsync TRƯỚC DeleteUserAsync
            _mockUserRepo.Verify(r => r.DeleteUserRelationsAsync(user), Times.Once);
            _mockUserRepo.Verify(r => r.DeleteUserAsync(user), Times.Once);
        }
    }
}
