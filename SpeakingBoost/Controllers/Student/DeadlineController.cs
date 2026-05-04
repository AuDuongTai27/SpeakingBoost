using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/deadlines")]
    [Authorize(Roles = "user")]
    public class DeadlineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DeadlineController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetStudentId()
        {
            var claim = User.FindFirst("StudentId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetDeadlines()
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            // 1. Lấy danh sách lớp học sinh thuộc vào
            var classIds = await _context.StudentClasses
                .Where(sc => sc.StudentId == studentId)
                .Select(sc => sc.ClassId)
                .ToListAsync();

            // 2. Lấy tất cả ClassExercise có deadline thuộc các lớp đó
            var classExercises = await _context.ClassExercises
                .Include(ce => ce.Exercise)
                .Include(ce => ce.SchoolClass)
                .Where(ce => classIds.Contains(ce.ClassId) && ce.Deadline.HasValue)
                .OrderBy(ce => ce.Deadline)
                .ToListAsync();

            // 3. Lấy các submission DEADLINE của học sinh (ClassExerciseId != null)
            var deadlineSubmissions = await _context.Submissions
                .Where(s => s.StudentId == studentId && s.ClassExerciseId != null)
                .Include(s => s.Scores)
                .ToListAsync();

            // 4. Build ViewModel
            var list = classExercises.Select(ce =>
            {
                // Chỉ tìm submission có ClassExerciseId trùng với deadline này
                var sub = deadlineSubmissions.FirstOrDefault(s => s.ClassExerciseId == ce.ClassExerciseId);

                string status;
                if (sub != null)
                    status = "Submitted";
                else if (ce.Deadline.HasValue && ce.Deadline.Value < DateTime.Now)
                    status = "Overdue";
                else
                    status = "Pending";

                return new DeadlineExerciseDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ExerciseId = ce.ExerciseId,
                    Title = ce.Exercise.Title,
                    Question = ce.Exercise.Question,
                    Type = ce.Exercise.Type,
                    Deadline = ce.Deadline,
                    ClassName = ce.SchoolClass.ClassName,
                    Status = status,
                    Score = sub?.Scores?.FirstOrDefault()?.Overall,
                    SubmissionId = sub?.SubmissionId ?? 0
                };
            }).ToList();

            return Ok(ApiResponse<List<DeadlineExerciseDto>>.SuccessResponse(list));
        }

        [HttpGet("{classExerciseId}")]
        public async Task<IActionResult> GetDeadlineQuestion(int classExerciseId)
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var ce = await _context.ClassExercises
                .Include(x => x.Exercise)
                .Include(x => x.SchoolClass)
                .FirstOrDefaultAsync(x => x.ClassExerciseId == classExerciseId);

            if (ce == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài tập."));

            var inClass = await _context.StudentClasses
                .AnyAsync(sc => sc.StudentId == studentId && sc.ClassId == ce.ClassId);
            if (!inClass) return Forbid();

            var attemptUsed = await _context.Submissions
                .CountAsync(s => s.StudentId == studentId && s.ClassExerciseId == classExerciseId);

            var latestSub = await _context.Submissions
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId && s.ClassExerciseId == classExerciseId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            string status;
            if (latestSub != null)
                status = "Submitted";
            else if (ce.Deadline.HasValue && ce.Deadline.Value < DateTime.Now)
                status = "Overdue";
            else
                status = "Pending";

            int part = ce.Exercise.Type.ToLower() switch
            {
                "part1" => 1,
                "part2" => 2,
                "part3" => 3,
                _ => 1
            };

            var dto = new DeadlineQuestionDto
            {
                ClassExerciseId = ce.ClassExerciseId,
                ExerciseId = ce.ExerciseId,
                Title = ce.Exercise.Title,
                Question = ce.Exercise.Question,
                Part = part,
                Deadline = ce.Deadline,
                ClassName = ce.SchoolClass.ClassName,
                MaxAttempts = ce.Exercise.MaxAttempts,
                AttemptUsed = attemptUsed,
                Status = status
            };

            return Ok(ApiResponse<DeadlineQuestionDto>.SuccessResponse(dto));
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(IFormFile audio, [FromForm] int exerciseId, [FromForm] int classExerciseId, [FromForm] int part)
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
                    ClassExerciseId = classExerciseId,
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
