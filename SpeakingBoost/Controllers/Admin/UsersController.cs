using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Services;

// ================================================================
// 📌 VÍ DỤ MẪU PHASE 2 — ADMIN API
// ================================================================
// So sánh MVC cũ ↔ API mới:
//
//  MVC (UserManagementController):             API (UsersController):
//  ─────────────────────────────────────────   ────────────────────────────────────────
//  GET  /Admin/UserManagement/Index         →  GET    /api/admin/users
//  POST /Admin/UserManagement/CreateUser    →  POST   /api/admin/users
//  GET  /Admin/UserManagement/Edit/5        →  GET    /api/admin/users/5
//  POST /Admin/UserManagement/Edit/5        →  PUT    /api/admin/users/5
//  POST /Admin/UserManagement/DeleteUser/5  →  DELETE /api/admin/users/5
//
//  Khác biệt:
//  - Không có return View(), RedirectToAction(), TempData, ViewBag
//  - Không có [ValidateAntiForgeryToken]
//  - Tất cả input qua [FromBody] DTO, output qua ApiResponse<T>
//  - Phân quyền qua [Authorize(Roles = "superadmin")]
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "superadmin")] // ← Thay [Authorize(Roles = "SuperAdmin")] của MVC
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoginServices _loginServices; // Dùng HashPassword

        public UsersController(ApplicationDbContext context, ILoginServices loginServices)
        {
            _context = context;
            _loginServices = loginServices;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/users
        // MVC cũ: Index() → return View(users)
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .Select(u => new UserDto
                {
                    UserId    = u.UserId,
                    FullName  = u.FullName,
                    Email     = u.Email,
                    Role      = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<UserDto>>.SuccessResponse(users));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/users/{id}
        // MVC cũ: Edit(id) → return View(user)
        // ────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy người dùng."));

            var dto = new UserDto
            {
                UserId    = user.UserId,
                FullName  = user.FullName,
                Email     = user.Email,
                Role      = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserDto>.SuccessResponse(dto));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/users
        // Body: CreateUserDto
        // MVC cũ: CreateUser(model) → _context.Add(user) → RedirectToAction(Index)
        // ────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Kiểm tra email đã tồn tại (giống MVC)
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict(ApiResponse<object>.ErrorResponse("Email này đã được sử dụng."));

            var user = new User
            {
                FullName     = dto.FullName,
                Email        = dto.Email.ToLower().Trim(),
                Role         = dto.Role,
                PasswordHash = _loginServices.HashPassword(dto.Password),
                CreatedAt    = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = new UserDto { UserId = user.UserId, FullName = user.FullName, Email = user.Email, Role = user.Role, CreatedAt = user.CreatedAt };

            // Trả 201 Created kèm location header — chuẩn REST
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId },
                ApiResponse<UserDto>.SuccessResponse(result, "Tạo người dùng thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/users/{id}
        // Body: UpdateUserDto
        // MVC cũ: Edit(id, user) → _context.Update(user) → RedirectToAction(Index)
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
            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy người dùng."));

            // Kiểm tra email trùng (loại trừ chính user)
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != id))
                return Conflict(ApiResponse<object>.ErrorResponse("Email này đã được sử dụng."));

            user.FullName = dto.FullName;
            user.Email    = dto.Email.ToLower().Trim();
            user.Role     = dto.Role;

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật thông tin thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/users/{id}
        // MVC cũ: DeleteUser(id) → xóa cascade → RedirectToAction(Index)
        // ────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Vocabularies)
                .Include(u => u.StudentClasses)
                .Include(u => u.Submissions)
                .Include(u => u.Notifications)
                .Include(u => u.TaughtClasses)
                .Include(u => u.CreatedExercises)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy người dùng."));

            try
            {
                // Xóa dữ liệu liên quan (giữ nguyên logic từ MVC)
                if (user.Vocabularies?.Any() == true)   _context.Vocabulary.RemoveRange(user.Vocabularies);
                if (user.StudentClasses?.Any() == true) _context.StudentClasses.RemoveRange(user.StudentClasses);
                if (user.Notifications?.Any() == true)  _context.Notifications.RemoveRange(user.Notifications);
                if (user.Submissions?.Any() == true)    _context.Submissions.RemoveRange(user.Submissions);

                if (user.CreatedExercises?.Any() == true)
                {
                    var exerciseIds = user.CreatedExercises.Select(e => e.ExerciseId).ToList();
                    var related = _context.Submissions.Where(s => exerciseIds.Contains(s.ExerciseId));
                    _context.Submissions.RemoveRange(related);
                    _context.Exercises.RemoveRange(user.CreatedExercises);
                }

                if (user.TaughtClasses?.Any() == true)
                    _context.RemoveRange(user.TaughtClasses);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // 204 No Content — chuẩn REST cho DELETE thành công
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi hệ thống khi xóa.", new List<string> { ex.Message }));
            }
        }
    }
}
