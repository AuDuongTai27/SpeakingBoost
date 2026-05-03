using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Auth;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Services;

namespace SpeakingBoost.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoginServices _loginServices;

        public ProfileController(ApplicationDbContext context, ILoginServices loginServices)
        {
            _context = context;
            _loginServices = loginServices;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirst("StudentId")?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác thực."));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Người dùng không tồn tại."));

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var userIdStr = User.FindFirst("StudentId")?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác thực."));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Người dùng không tồn tại."));

            user.FullName = request.FullName;

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = _loginServices.HashPassword(request.Password);
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật thông tin thành công!"));
        }
    }
}
