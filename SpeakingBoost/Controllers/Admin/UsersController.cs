using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Services;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "admin")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoginServices _loginServices;

        public UsersController(ApplicationDbContext context, ILoginServices loginServices)
        {
            _context = context;
            _loginServices = loginServices;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/users
        // Chỉ hiển thị học sinh (user), không hiển thị admin
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Where(u => u.Role == "user")  // Chỉ lấy học sinh
                .OrderBy(u => u.FullName)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(ApiResponse<List<UserDto>>.SuccessResponse(users));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/users/{id}
        // ────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "user")
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy học sinh."));

            return Ok(ApiResponse<UserDto>.SuccessResponse(new UserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            }));
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
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(ApiResponse<object>.ErrorResponse("Email này đã được sử dụng."));

            // Tạo mới luôn là học sinh (user)
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email.ToLower().Trim(),
                Role = "user",  // Luôn tạo với role = user
                PasswordHash = _loginServices.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = new UserDto { UserId = user.UserId, FullName = user.FullName, Email = user.Email, Role = user.Role };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId },
                ApiResponse<UserDto>.SuccessResponse(result, "Tạo học sinh thành công!"));
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
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Role != "user")
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy học sinh."));

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
                return Conflict(ApiResponse<object>.ErrorResponse("Email này đã được sử dụng."));

            user.FullName = dto.FullName;
            user.Email = dto.Email.ToLower().Trim();
            // Không cho phép thay đổi Role, luôn giữ nguyên "user"

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật thông tin thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/users/{id}
        // ────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.StudentClasses)
                .Include(u => u.Submissions)
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null || user.Role != "user")
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy học sinh."));

            try
            {
                if (user.StudentClasses?.Any() == true) _context.StudentClasses.RemoveRange(user.StudentClasses);
                if (user.Notifications?.Any() == true) _context.Notifications.RemoveRange(user.Notifications);
                if (user.Submissions?.Any() == true) _context.Submissions.RemoveRange(user.Submissions);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi hệ thống khi xóa.", new List<string> { ex.Message }));
            }
        }
    }
}