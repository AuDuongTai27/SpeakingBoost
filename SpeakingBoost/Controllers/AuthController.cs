using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Auth;
using SpeakingBoost.Services.Interfaces.Auth;
using SpeakingBoost.Services.Implementations.Auth;
using SpeakingBoost.Services.Interfaces.Email;
using SpeakingBoost.Services.Implementations.Email;
using SpeakingBoost.Helpers;
using System.Security.Claims;

namespace SpeakingBoost.Controllers
{
    /// <summary>
    /// AuthController — tương đương LoginController bên MVC
    ///
    /// MVC cũ:  POST /Login/LoginToSystem  → Dùng Cookie Auth
    /// API mới: POST /api/auth/login       → Trả JWT Token
    ///
    /// Điểm khác biệt chính:
    ///   - Không dùng HttpContext.SignInAsync() / SignOutAsync()
    ///   - Không có [ValidateAntiForgeryToken]
    ///   - Trả JSON thay vì return View() / RedirectToAction()
    ///   - Token chứa đầy đủ claims giống Cookie cũ (StudentId, Role, Email...)
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILoginServices _loginServices;
        private readonly IJwtService    _jwtService;
        private readonly IEmailService  _emailService;

        public AuthController(
            ILoginServices loginServices,
            IJwtService    jwtService,
            IEmailService  emailService)
        {
            _loginServices = loginServices;
            _jwtService    = jwtService;
            _emailService  = emailService;
        }

        // ============================================================
        // POST /api/auth/login
        // Body: { "email": "...", "password": "..." }
        // ============================================================
        // MVC cũ: LoginToSystem() → HttpContext.SignInAsync(Cookie) → RedirectToAction
        // API mới: → GenerateToken(user) → return Ok(token)
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {   
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ", errors, 400));
            }

            try
            {
                var account = _loginServices.Login(request.Email, request.Password);

                if (account == null)
                    return Unauthorized(BaseResponse<object>.Fail("Tài khoản hoặc mật khẩu không chính xác.", 401));

                // Tạo JWT token (thay thế HttpContext.SignInAsync)
                string token = _jwtService.GenerateToken(account);
                string role  = account.Role?.Trim().ToLower() ?? "";

                // Xác định redirect URL theo role sử dụng Helper
                // CODE CŨ:
                // string redirectUrl = role switch
                // {
                //     "user"  => "/student/homepage.html",
                //     "admin" => "/admin/dashboard.html",
                //     _       => "/login.html"
                // };
                string redirectUrl = ClaimHelper.GetRedirectUrl(role);

                var response = new LoginResponse
                {
                    Token       = token,
                    Role        = role,
                    UserId      = account.UserId,
                    FullName    = account.FullName,
                    Email       = account.Email,
                    RedirectUrl = redirectUrl
                };

                return Ok(BaseResponse<LoginResponse>.Ok(response, "Đăng nhập thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi hệ thống khi đăng nhập.", new List<string> { ex.Message }, 500));
            }
        }

        // ============================================================
        // POST /api/auth/forgot-password
        // Body: { "email": "..." }
        // ============================================================
        // MVC cũ: HandleForgotPassword() → TempData["SuccessMessage"]
        // API mới: → return Ok / BadRequest với message JSON
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ", errors, 400));
            }

            try
            {
                var user = _loginServices.GetUserByEmail(request.Email);
                if (user == null)
                    return BadRequest(BaseResponse<object>.Fail("Email không được cấp phép hoặc nhập sai.", 400));

                // Tạo mật khẩu mới ngẫu nhiên
                string newPassword = Guid.NewGuid().ToString()[..8];
                string hashedPass  = _loginServices.HashPassword(newPassword);
                bool updated       = _loginServices.UpdatePassword(user.UserId, hashedPass);

                if (!updated)
                    return BadRequest(BaseResponse<object>.Fail("Không thể cập nhật mật khẩu.", 400));

                // Gửi email mật khẩu mới
                await _emailService.SendRecoveredPassword(user.Email, newPassword);

                return Ok(BaseResponse<object>.Ok("Mật khẩu mới đã được gửi về email của bạn. Vui lòng kiểm tra hộp thư!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi hệ thống.", new List<string> { ex.Message }, 500));
            }
        }

        // ============================================================
        // GET /api/auth/me
        // Header: Authorization: Bearer <token>
        // ============================================================
        // Dùng để frontend kiểm tra token còn hợp lệ không
        // và lấy thông tin user hiện tại (role, userId, fullName...)
        // Frontend guard sẽ gọi endpoint này mỗi lần load trang protected
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var role     = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            // CODE CŨ: var userId   = User.FindFirst("StudentId")?.Value ?? "0";
            var userId   = User.GetStudentId() ?? 0;
            var fullName = User.FindFirst(ClaimTypes.Name)?.Value ?? "";
            var email    = User.FindFirst(ClaimTypes.Email)?.Value ?? "";

            // CODE CŨ:
            // string redirectUrl = role.Trim().ToLower() switch
            // {
            //     "user"  => "/student/homepage.html",
            //     "admin" => "/admin/dashboard.html",
            //     _       => "/login.html"
            // };
            string redirectUrl = ClaimHelper.GetRedirectUrl(role);

            return Ok(BaseResponse<object>.Ok(new
            {
                UserId      = userId,
                FullName    = fullName,
                Email       = email,
                Role        = role,
                RedirectUrl = redirectUrl
            }));
        }
    }
}
