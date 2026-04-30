using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Models.Entities;
using System.Security.Claims;

// ================================================================
// 📌 VÍ DỤ MẪU PHASE 3 — STUDENT API
// ================================================================
// So sánh MVC cũ ↔ API mới:
//
//  MVC (PracticeSpeakingController):             API (PracticeController):
//  ────────────────────────────────────────────  ────────────────────────────────────────────
//  GET  /Student/PracticeSpeaking/Index       →  GET  /api/student/practice/topics?part=1
//  GET  /Student/PracticeSpeaking/Question/5  →  GET  /api/student/practice/topics/5?part=1
//  POST /Student/PracticeSpeaking/Analyze     →  POST /api/student/submissions (multipart)
//  (polling trạng thái — không có bên MVC)    →  GET  /api/student/submissions/{id}/status
//
//  Khác biệt:
//  - Lấy StudentId từ JWT claim thay vì User.FindFirst("StudentId") Cookie
//    (Code đọc claim GIỐNG HỆT nhau vì JWT cũng có Claims)
//  - Không có return View() — trả JSON DTO
//  - [Authorize(Roles = "student")] thay vì Area("Student")
// ================================================================

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/practice")]
    [Authorize(Roles = "student")]
    public class PracticeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PracticeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper lấy StudentId từ JWT token — y chang MVC cũ
        private int? GetStudentId()
        {
            var claim = User.FindFirst("StudentId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/student/practice/topics?part=1
        // MVC cũ: Index() → return View(topics)
        // ────────────────────────────────────────────────────────────
        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics([FromQuery] int part = 0)
        {
            var partKey = $"part{part}";

            var topics = await _context.VocabularyTopics
                .AsNoTracking()
                .Select(t => new PracticeTopicDto
                {
                    Id            = t.TopicId,
                    Title         = t.Name,
                    ForecastLabel = t.Description ?? "Bộ đề dự đoán",
                    ForecastDate  = t.CreatedAt.ToString("dd/MM/yyyy"),
                    QuestionCount = t.Exercises!.Count(e =>
                        part == 0 || e.Type.ToLower() == partKey)
                })
                .Where(t => t.QuestionCount > 0)
                .ToListAsync();

            return Ok(ApiResponse<List<PracticeTopicDto>>.SuccessResponse(topics));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/student/practice/topics/{id}?part=1
        // MVC cũ: PracticeQuestion(id) → return View(vm)
        // ────────────────────────────────────────────────────────────
        [HttpGet("topics/{id}")]
        public async Task<IActionResult> GetTopicQuestions(int id, [FromQuery] int part = 0)
        {
            var studentId = GetStudentId();
            if (studentId == null)
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var topic = await _context.VocabularyTopics.AsNoTracking()
                .Where(t => t.TopicId == id)
                .Select(t => new { t.TopicId, t.Name })
                .FirstOrDefaultAsync();

            if (topic == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            var partKey = $"part{part}";

            // Query kết hợp Exercise + số lần nộp — giống logic MVC cũ
            var questions = await (
                from e in _context.Exercises.AsNoTracking()
                where e.TopicId == id
                      && (part == 0 || e.Type.ToLower() == partKey)
                join s in _context.Submissions.AsNoTracking()
                    .Where(x => x.StudentId == studentId)
                    on e.ExerciseId equals s.ExerciseId into sg
                orderby e.ExerciseId
                select new PracticeQuestionDto
                {
                    ExerciseId   = e.ExerciseId,
                    Title        = e.Title,
                    Question     = e.Question,
                    Type         = e.Type,
                    MaxAttempts  = e.MaxAttempts,
                    AttemptUsed  = sg.Count()
                }
            ).ToListAsync();

            return Ok(ApiResponse<List<PracticeQuestionDto>>.SuccessResponse(questions));
        }


        // ────────────────────────────────────────────────────────────
        // POST /api/student/practice/analyze
        // Gửi audio (multipart/form-data)
        // ────────────────────────────────────────────────────────────
        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(IFormFile audio, [FromForm] int exerciseId, [FromForm] int part)
        {
            if (audio == null || audio.Length == 0) return BadRequest(ApiResponse<object>.ErrorResponse("File audio không tồn tại."));

            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            string? filePath = null;
            var queued = false;
            try
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "audio");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(audio.FileName)}";
                filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await audio.CopyToAsync(stream);

                var audioPath = $"/audio/{fileName}";

                var submission = new Submission
                {
                    StudentId = studentId.Value,
                    ExerciseId = exerciseId,
                    ClassExerciseId = null,
                    AudioPath = audioPath,
                    Status = ProcessingStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                var queue = HttpContext.RequestServices.GetRequiredService<SpeakingBoost.Services.Background.BackgroundQueue>();
                queued = queue.TryQueueBackgroundWorkItem(submission.SubmissionId);

                if (!queued)
                {
                    submission.Status = ProcessingStatus.Failed;
                    submission.ErrorMessage = "Hệ thống đang bận. Vui lòng thử lại sau ít phút.";
                    await _context.SaveChangesAsync();
                    return StatusCode(429, ApiResponse<object>.ErrorResponse("Hệ thống đang bận, vui lòng thử lại sau."));
                }

                return Ok(ApiResponse<SubmitAudioResponse>.SuccessResponse(new SubmitAudioResponse
                {
                    SubmissionId = submission.SubmissionId,
                    Status = "Pending",
                    Message = "Đang xử lý trong nền, vui lòng chờ..."
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi hệ thống khi nộp bài.", new List<string> { ex.Message }));
            }
        }
    }
}
