using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Auth;
using SpeakingBoost.Services.Auth;
using SpeakingBoost.Helpers;

namespace SpeakingBoost.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        // CODE CŨ:
        // private readonly ApplicationDbContext _context;
        // private readonly ILoginServices _loginServices;
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            // CODE CŨ:
            // var userIdStr = User.FindFirst("StudentId")?.Value;
            // if (!int.TryParse(userIdStr, out int userId))
            //     return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác thực."));
            //
            // var user = await _context.Users.FindAsync(userId);
            // if (user == null)
            //     return NotFound(ApiResponse<object>.ErrorResponse("Người dùng không tồn tại."));
            
            var userId = User.GetStudentId();
            if (userId == null)
                return Unauthorized(BaseResponse<object>.Fail("Không thể xác thực.", 401));

            var profile = await _profileService.GetProfileAsync(userId.Value);
            if (profile == null)
                return NotFound(BaseResponse<object>.Fail("Người dùng không tồn tại.", 404));

            return Ok(BaseResponse<UserProfileDto>.Ok(profile));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ", errors, 400));
            }

            // CODE CŨ:
            // var userIdStr = User.FindFirst("StudentId")?.Value;
            // if (!int.TryParse(userIdStr, out int userId))
            //     return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác thực."));
            //
            // var user = await _context.Users.FindAsync(userId);
            // if (user == null)
            //     return NotFound(ApiResponse<object>.ErrorResponse("Người dùng không tồn tại."));
            //
            // user.FullName = request.FullName;
            // if (!string.IsNullOrEmpty(request.Password))
            // {
            //     user.PasswordHash = _loginServices.HashPassword(request.Password);
            // }
            // await _context.SaveChangesAsync();

            var userId = User.GetStudentId();
            if (userId == null)
                return Unauthorized(BaseResponse<object>.Fail("Không thể xác thực.", 401));

            var success = await _profileService.UpdateProfileAsync(userId.Value, request);
            if (!success)
                return NotFound(BaseResponse<object>.Fail("Người dùng không tồn tại.", 404));

            return Ok(BaseResponse<object>.Ok("Cập nhật thông tin thành công!"));
        }
    }
}
