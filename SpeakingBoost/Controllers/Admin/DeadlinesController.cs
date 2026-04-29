using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Services.Email;

// ================================================================
// DeadlinesController — tương đương DeadlineController (MVC)
//
// MVC cũ:                                          API mới:
// ───────────────────────────────────────────────  ──────────────────────────────────────────
// GET  /Admin/Deadline/Index                     →  GET    /api/admin/deadlines
// POST /Admin/Deadline/Index (bulk assign)       →  POST   /api/admin/deadlines/bulk
// POST /Admin/Deadline/DeleteDeadlineFromIndex   →  DELETE /api/admin/deadlines/{id}
// GET  /Admin/Deadline/Manage?exerciseId=5       →  GET    /api/admin/deadlines/exercise/5
// POST /Admin/Deadline/SaveDeadline              →  POST   /api/admin/deadlines/exercise/{exerciseId}/class
// POST /Admin/Deadline/RemoveDeadline            →  DELETE /api/admin/deadlines/{classExerciseId}
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "teacher,superadmin")]
    public class DeadlinesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService        _emailService;

        public DeadlinesController(ApplicationDbContext context, IEmailService emailService)
        {
            _context      = context;
            _emailService = emailService;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/deadlines
        // MVC cũ: Index() — trả danh sách deadline đang chạy + dropdowns
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/deadlines")]
        public async Task<IActionResult> GetActiveDeadlines()
        {
            var deadlines = await _context.ClassExercises
                .Include(ce => ce.SchoolClass)
                .Include(ce => ce.Exercise)
                .Where(ce => ce.Deadline.HasValue)
                .OrderByDescending(ce => ce.Deadline)
                .Select(ce => new ActiveDeadlineDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ClassId         = ce.ClassId,
                    ClassName       = ce.SchoolClass.ClassName,
                    ExerciseId      = ce.ExerciseId,
                    ExerciseTitle   = ce.Exercise.Title,
                    Deadline        = ce.Deadline
                })
                .ToListAsync();

            // Kèm dữ liệu dropdown cho frontend
            var classes = await _context.Classes
                .OrderBy(c => c.ClassName)
                .Select(c => new { c.ClassId, c.ClassName })
                .ToListAsync();

            var topics = await _context.VocabularyTopics
                .OrderBy(t => t.Name)
                .Select(t => new { t.TopicId, t.Name })
                .ToListAsync();

            var allExercises = await _context.Exercises
                .Include(e => e.VocabularyTopic)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.ExerciseId,
                    e.Title,
                    e.Type,
                    TopicName = e.VocabularyTopic != null ? e.VocabularyTopic.Name : null
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                ActiveDeadlines = deadlines,
                Classes         = classes,
                Topics          = topics,
                AllExercises    = allExercises
            }));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/deadlines/bulk
        // Body: BulkAssignDeadlineDto
        // MVC cũ: POST Index(model) — gán hàng loạt bài tập cho lớp + gửi email
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/deadlines/bulk")]
        public async Task<IActionResult> BulkAssign([FromBody] BulkAssignDeadlineDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Validate mode
            if (dto.AssignMode == "Topic" && dto.SelectedTopicId == null)
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn Chủ đề."));

            if (dto.AssignMode == "Custom" && (dto.SelectedExerciseIds == null || !dto.SelectedExerciseIds.Any()))
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn ít nhất 1 bài tập."));

            // Lấy danh sách exercises cần gán
            List<Exercise> exercisesToAssign;
            if (dto.AssignMode == "Topic")
                exercisesToAssign = await _context.Exercises.Where(e => e.TopicId == dto.SelectedTopicId).ToListAsync();
            else
                exercisesToAssign = await _context.Exercises.Where(e => dto.SelectedExerciseIds.Contains(e.ExerciseId)).ToListAsync();

            if (!exercisesToAssign.Any())
                return BadRequest(ApiResponse<object>.ErrorResponse("Không tìm thấy bài tập nào để gán."));

            int count = 0;
            foreach (var exercise in exercisesToAssign)
            {
                var existing = await _context.ClassExercises
                    .FirstOrDefaultAsync(ce => ce.ClassId == dto.ClassId && ce.ExerciseId == exercise.ExerciseId);

                if (existing != null)
                    existing.Deadline = dto.Deadline;
                else
                    _context.ClassExercises.Add(new ClassExercise
                    {
                        ClassId    = dto.ClassId,
                        ExerciseId = exercise.ExerciseId,
                        Deadline   = dto.Deadline
                    });
                count++;
            }

            await _context.SaveChangesAsync();

            // Gửi email thông báo — giống MVC
            var className = (await _context.Classes.FindAsync(dto.ClassId))?.ClassName ?? "";
            string subject = dto.AssignMode == "Topic"
                ? $"Bài tập mới chủ đề: {(await _context.VocabularyTopics.FindAsync(dto.SelectedTopicId))?.Name}"
                : $"Bạn có {count} bài tập mới";

            await SendBulkEmailsToClass(dto.ClassId, subject, className, dto.Deadline, count);

            return Ok(ApiResponse<object>.SuccessResponse($"Đã gán thành công {count} bài tập cho lớp {className}!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/deadlines/{id}
        // MVC cũ: DeleteDeadlineFromIndex(id) — xóa 1 ClassExercise
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/deadlines/{id}")]
        public async Task<IActionResult> DeleteDeadline(int id)
        {
            var assignment = await _context.ClassExercises.FindAsync(id);
            if (assignment == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy dữ liệu để xóa."));

            _context.ClassExercises.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/deadlines/exercise/{exerciseId}
        // MVC cũ: Manage(exerciseId) — xem tất cả lớp đã gán bài tập này
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/deadlines/exercise/{exerciseId}")]
        public async Task<IActionResult> GetByExercise(int exerciseId)
        {
            var exercise = await _context.Exercises.FindAsync(exerciseId);
            if (exercise == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài tập."));

            var assignments = await _context.ClassExercises
                .Include(ce => ce.SchoolClass)
                .Where(ce => ce.ExerciseId == exerciseId)
                .OrderBy(ce => ce.SchoolClass.ClassName)
                .Select(ce => new ActiveDeadlineDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ClassId         = ce.ClassId,
                    ClassName       = ce.SchoolClass.ClassName,
                    ExerciseId      = ce.ExerciseId,
                    ExerciseTitle   = exercise.Title,
                    Deadline        = ce.Deadline
                })
                .ToListAsync();

            var classes = await _context.Classes
                .OrderBy(c => c.ClassName)
                .Select(c => new { c.ClassId, c.ClassName })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                ExerciseId    = exercise.ExerciseId,
                ExerciseTitle = exercise.Title,
                Assignments   = assignments,
                Classes       = classes
            }));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/deadlines/exercise/{exerciseId}/class
        // Body: SaveDeadlineDto
        // MVC cũ: SaveDeadline(model) — upsert deadline cho (exerciseId, classId) + gửi email
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/deadlines/exercise/{exerciseId}/class")]
        public async Task<IActionResult> SaveDeadline(int exerciseId, [FromBody] SaveDeadlineDto dto)
        {
            var assignment = await _context.ClassExercises
                .Include(ce => ce.SchoolClass)
                .Include(ce => ce.Exercise)
                .FirstOrDefaultAsync(ce => ce.ExerciseId == exerciseId && ce.ClassId == dto.ClassId);

            string message;
            if (assignment != null)
            {
                assignment.Deadline = dto.Deadline;
                message = "Đã cập nhật deadline thành công!";
            }
            else
            {
                var newAssignment = new ClassExercise
                {
                    ClassId    = dto.ClassId,
                    ExerciseId = exerciseId,
                    Deadline   = dto.Deadline
                };
                _context.ClassExercises.Add(newAssignment);
                await _context.SaveChangesAsync();

                // Reload để lấy navigation properties cho email
                assignment = await _context.ClassExercises
                    .Include(ce => ce.SchoolClass)
                    .Include(ce => ce.Exercise)
                    .FirstOrDefaultAsync(ce => ce.ClassExerciseId == newAssignment.ClassExerciseId);

                message = "Đã gán bài tập thành công!";
            }

            await _context.SaveChangesAsync();

            // Gửi email nếu có deadline
            if (dto.Deadline.HasValue && assignment != null)
            {
                await SendEmailsToClass(
                    dto.ClassId,
                    assignment.Exercise?.Title ?? "",
                    assignment.SchoolClass?.ClassName ?? "",
                    dto.Deadline.Value);
            }

            return Ok(ApiResponse<object>.SuccessResponse(message));
        }

        // ────────────────────────────────────────────────────────────
        // HELPERS — giống private methods bên MVC
        // ────────────────────────────────────────────────────────────

        private async Task SendEmailsToClass(int classId, string exerciseTitle, string className, DateTime deadline)
        {
            await SendBulkEmailsToClass(classId, exerciseTitle, className, deadline, 1);
        }

        private async Task SendBulkEmailsToClass(int classId, string subjectContent, string className, DateTime deadline, int count)
        {
            var students = await _context.StudentClasses
                .Include(sc => sc.Student)
                .Where(sc => sc.ClassId == classId && sc.Student.Role == "Student")
                .Select(sc => sc.Student)
                .ToListAsync();

            foreach (var student in students)
            {
                try
                {
                    await _emailService.SendDeadlineNotification(
                        student.Email,
                        subjectContent + (count > 1 ? $" (Tổng {count} bài)" : ""),
                        className,
                        deadline);
                }
                catch { /* Bỏ qua lỗi email từng học sinh — giống MVC */ }
            }
        }
    }
}
