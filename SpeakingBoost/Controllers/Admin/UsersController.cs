using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/users
        // Chỉ hiển thị học sinh (user), không hiển thị admin
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(BaseResponse<List<UserDto>>.Ok(users));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/users/{id}
        // ────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(BaseResponse<UserDto>.Ok(user));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/users
        // Body: CreateUserDto (không cần trường Role)
        // ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                var user = await _userService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUser), new { id = user.UserId },
                    BaseResponse<UserDto>.Ok(user, "Tạo học sinh thành công!"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(BaseResponse<object>.Fail(ex.Message, 409));
            }
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/users/{id}
        // Body: UpdateUserDto (không cần trường Role)
        // ────────────────────────────────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                await _userService.UpdateUserAsync(id, dto);
                return Ok(BaseResponse<object>.Ok("Cập nhật thông tin thành công!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(BaseResponse<object>.Fail(ex.Message, 409));
            }
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/users/{id}
        // ────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi hệ thống khi xóa: " + ex.Message, 500));
            }
        }
    }
}