using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Models.DTOs.Auth;
using SpeakingBoost.Services.Interfaces.Auth;
using SpeakingBoost.Helpers;

namespace SpeakingBoost.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {     
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
