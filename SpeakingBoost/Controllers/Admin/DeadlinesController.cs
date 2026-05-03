using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Services.Email;

// ================================================================
// DeadlinesController — Quản lý giao bài theo chủ đề
//
// Deadline chỉ được giao theo Topic (chủ đề).
// 1 Topic có nhiều câu hỏi → tất cả được giao cùng deadline cho lớp.
// Không hỗ trợ giao từng bài lẻ (Custom mode đã bỏ).
//
// GET    /api/admin/deadlines              — xem deadlines đang chạy
// POST   /api/admin/deadlines/assign       — giao chủ đề cho lớp
// DELETE /api/admin/deadlines/{id}         — xóa 1 ClassExercise
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "admin")]
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
        // Trả danh sách deadline đang chạy + dữ liệu dropdown cho frontend
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/deadlines")]
        public async Task<IActionResult> GetActiveDeadlines()
        {
            var deadlines = await _context.ClassExercises
                .Include(ce => ce.SchoolClass)
                .Include(ce => ce.Exercise)
                    .ThenInclude(e => e.VocabularyTopic)
                .Where(ce => ce.Deadline.HasValue)
                .OrderByDescending(ce => ce.Deadline)
                .Select(ce => new ActiveDeadlineDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ClassId         = ce.ClassId,
                    ClassName       = ce.SchoolClass.ClassName,
                    ExerciseId      = ce.ExerciseId,
                    ExerciseTitle   = ce.Exercise.Title,
                    TopicName       = ce.Exercise.VocabularyTopic != null ? ce.Exercise.VocabularyTopic.Name : null,
                    Deadline        = ce.Deadline
                })
                .ToListAsync();

            // Dropdown: danh sách lớp
            var classes = await _context.Classes
                .OrderBy(c => c.ClassName)
                .Select(c => new { c.ClassId, c.ClassName })
                .ToListAsync();

            // Dropdown: danh sách topic (mỗi topic có nhiều câu hỏi)
            var topics = await _context.VocabularyTopics
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.TopicId,
                    t.Name,
                    t.Description,
                    ExerciseCount = t.Exercises != null ? t.Exercises.Count : 0
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                ActiveDeadlines = deadlines,
                Classes         = classes,
                Topics          = topics
            }));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/deadlines/assign
        // Body: AssignTopicDeadlineDto
        // Giao toàn bộ câu hỏi của 1 Topic cho 1 Lớp với cùng Deadline
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/deadlines/assign")]
        public async Task<IActionResult> AssignTopicDeadline([FromBody] AssignTopicDeadlineDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Lấy tất cả exercises thuộc topic
            var exercises = await _context.Exercises
                .Where(e => e.TopicId == dto.TopicId)
                .ToListAsync();

            if (!exercises.Any())
                return BadRequest(ApiResponse<object>.ErrorResponse("Chủ đề này chưa có câu hỏi nào."));

            // Kiểm tra class tồn tại
            var schoolClass = await _context.Classes.FindAsync(dto.ClassId);
            if (schoolClass == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy lớp học."));

            // Kiểm tra topic tồn tại
            var topic = await _context.VocabularyTopics.FindAsync(dto.TopicId);
            if (topic == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            int added = 0, updated = 0;
            foreach (var exercise in exercises)
            {
                var existing = await _context.ClassExercises
                    .FirstOrDefaultAsync(ce => ce.ClassId == dto.ClassId && ce.ExerciseId == exercise.ExerciseId);

                if (existing != null)
                {
                    existing.Deadline = dto.Deadline;
                    updated++;
                }
                else
                {
                    _context.ClassExercises.Add(new ClassExercise
                    {
                        ClassId    = dto.ClassId,
                        ExerciseId = exercise.ExerciseId,
                        Deadline   = dto.Deadline
                    });
                    added++;
                }
            }

            await _context.SaveChangesAsync();

            // Gửi email thông báo cho học viên trong lớp
            await SendTopicDeadlineNotification(dto.ClassId, topic.Name, schoolClass.ClassName, dto.Deadline, exercises.Count);

            string message = added > 0 && updated > 0
                ? $"Đã gán {added} câu hỏi mới và cập nhật {updated} câu hỏi của chủ đề '{topic.Name}' cho lớp {schoolClass.ClassName}!"
                : added > 0
                    ? $"Đã gán {added} câu hỏi của chủ đề '{topic.Name}' cho lớp {schoolClass.ClassName}!"
                    : $"Đã cập nhật deadline cho {updated} câu hỏi của chủ đề '{topic.Name}'.";

            return Ok(ApiResponse<object>.SuccessResponse(message));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/deadlines/{id}
        // Xóa 1 ClassExercise (1 câu hỏi khỏi deadline của lớp)
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/deadlines/{id}")]
        public async Task<IActionResult> DeleteDeadline(int id)
        {
            var assignment = await _context.ClassExercises.FindAsync(id);
            if (assignment == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy deadline để xóa."));

            _context.ClassExercises.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/deadlines/topic/{topicId}/class/{classId}
        // Xóa toàn bộ deadline của 1 topic khỏi 1 lớp
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/deadlines/topic/{topicId}/class/{classId}")]
        public async Task<IActionResult> DeleteTopicDeadlineFromClass(int topicId, int classId)
        {
            var exerciseIds = await _context.Exercises
                .Where(e => e.TopicId == topicId)
                .Select(e => e.ExerciseId)
                .ToListAsync();

            var assignments = await _context.ClassExercises
                .Where(ce => ce.ClassId == classId && exerciseIds.Contains(ce.ExerciseId))
                .ToListAsync();

            if (!assignments.Any())
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy deadline cần xóa."));

            _context.ClassExercises.RemoveRange(assignments);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse($"Đã xóa {assignments.Count} deadline của chủ đề khỏi lớp."));
        }

        // ────────────────────────────────────────────────────────────
        // HELPER — Gửi email thông báo deadline đến học viên trong lớp
        // ────────────────────────────────────────────────────────────
        private async Task SendTopicDeadlineNotification(int classId, string topicName, string className, DateTime deadline, int exerciseCount)
        {
            var students = await _context.StudentClasses
                .Include(sc => sc.Student)
                .Where(sc => sc.ClassId == classId && sc.Student.Role == "user")
                .Select(sc => sc.Student)
                .ToListAsync();

            string subject = $"Bài tập mới - Chủ đề: {topicName} ({exerciseCount} câu hỏi)";

            foreach (var student in students)
            {
                try
                {
                    await _emailService.SendDeadlineNotification(
                        student.Email,
                        subject,
                        className,
                        deadline);
                }
                catch { /* Bỏ qua lỗi email từng học sinh */ }
            }
        }
    }
}
