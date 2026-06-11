using Microsoft.Extensions.Configuration;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Services.Implementations.Auth;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace SpeakingBoost.Tests.Auth
{
    /// <summary>
    /// Unit Tests cho JwtService — kiểm tra token sinh ra hợp lệ
    /// </summary>
    public class JwtServiceTests
    {
        private readonly JwtService _service;
        private readonly User _testUser;

        public JwtServiceTests()
        {
            // Tạo IConfiguration giả với JWT settings
            var inMemorySettings = new Dictionary<string, string>
            {
                { "Jwt:Key",           "SpeakingBoost_SuperSecretKey_123456!" },
                { "Jwt:Issuer",        "SpeakingBoostTest" },
                { "Jwt:Audience",      "SpeakingBoostTest" },
                { "Jwt:ExpireMinutes", "60" }
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _service = new JwtService(config);

            _testUser = new User
            {
                UserId   = 1,
                FullName = "Test Student",
                Email    = "test@student.com",
                Role     = "user",
                PasswordHash = "hashed"
            };
        }

        // ─────────────────────────────────────────────────────
        // TC-L09 (JwtService): GenerateToken sinh ra chuỗi JWT hợp lệ (có 3 phần ngăn bởi dấu chấm)
        // JWT format: Header.Payload.Signature
        // ─────────────────────────────────────────────────────
        [Fact]
        public void GenerateToken_ValidUser_ReturnsValidJwtString()
        {
            // Act
            var token = _service.GenerateToken(_testUser);

            // Assert: JWT phải có đúng 3 phần (2 dấu chấm)
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
        }

        // ─────────────────────────────────────────────────────
        // TC-JWT-02: Token chứa đúng email và role trong claims
        // ─────────────────────────────────────────────────────
        [Fact]
        public void GenerateToken_ValidUser_ContainsCorrectClaims()
        {
            // Act
            var token = _service.GenerateToken(_testUser);

            // Parse token để đọc claims
            var handler    = new JwtSecurityTokenHandler();
            var jwtToken   = handler.ReadJwtToken(token);

            var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Value == _testUser.Email);
            var roleClaim  = jwtToken.Claims.FirstOrDefault(c => c.Value == "user");

            // Assert
            Assert.NotNull(emailClaim);
            Assert.NotNull(roleClaim);
        }

        // ─────────────────────────────────────────────────────
        // TC-JWT-03: Token chưa hết hạn ngay sau khi sinh
        // ─────────────────────────────────────────────────────
        [Fact]
        public void GenerateToken_NewToken_IsNotExpired()
        {
            // Act
            var token      = _service.GenerateToken(_testUser);
            var handler    = new JwtSecurityTokenHandler();
            var jwtToken   = handler.ReadJwtToken(token);

            // Assert: thời gian hết hạn phải lớn hơn thời gian hiện tại
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
        }
    }
}
