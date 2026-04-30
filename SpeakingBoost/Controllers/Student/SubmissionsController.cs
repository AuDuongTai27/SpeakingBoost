using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.EF;
using System.Text.Json;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/submissions")]
    [Authorize(Roles = "student")]
    public class SubmissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetStudentId()
        {
            var claim = User.FindFirst("StudentId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet("all-history")]
        public async Task<IActionResult> GetAllHistory()
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var submissions = await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId.Value)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var list = submissions.Select(s => new AttemptHistoryItemDto
            {
                SubmissionId = s.SubmissionId,
                ClassExerciseId = s.ClassExerciseId,
                ExerciseTitle = s.Exercise?.Title ?? "N/A",
                AttemptNumber = s.AttemptNumber,
                CreatedAt = s.CreatedAt,
                Overall = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                Status = s.Status.ToString(),
                ErrorMessage = s.ErrorMessage
            }).ToList();

            return Ok(ApiResponse<List<AttemptHistoryItemDto>>.SuccessResponse(list));
        }

        [HttpGet("practice-history")]
        public async Task<IActionResult> GetHistory([FromQuery] int exerciseId)
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var submissions = await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId.Value && s.ExerciseId == exerciseId && s.ClassExerciseId == null)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var list = submissions.Select(s => new AttemptHistoryItemDto
            {
                SubmissionId = s.SubmissionId,
                ClassExerciseId = s.ClassExerciseId,
                ExerciseTitle = s.Exercise?.Title ?? "N/A",
                AttemptNumber = s.AttemptNumber,
                CreatedAt = s.CreatedAt,
                Overall = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                Status = s.Status.ToString(),
                ErrorMessage = s.ErrorMessage
            }).ToList();

            return Ok(ApiResponse<List<AttemptHistoryItemDto>>.SuccessResponse(list));
        }

        [HttpGet("deadline-history")]
        public async Task<IActionResult> GetDeadlineHistory([FromQuery] int classExerciseId)
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var submissions = await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId.Value && s.ClassExerciseId == classExerciseId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var list = submissions.Select(s => new AttemptHistoryItemDto
            {
                SubmissionId = s.SubmissionId,
                ClassExerciseId = s.ClassExerciseId,
                ExerciseTitle = s.Exercise?.Title ?? "N/A",
                AttemptNumber = s.AttemptNumber,
                CreatedAt = s.CreatedAt,
                Overall = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                Status = s.Status.ToString(),
                ErrorMessage = s.ErrorMessage
            }).ToList();

            return Ok(ApiResponse<List<AttemptHistoryItemDto>>.SuccessResponse(list));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAttemptDetail(int id)
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var submission = await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .FirstOrDefaultAsync(s => s.SubmissionId == id && s.StudentId == studentId.Value);

            if (submission == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài nộp."));

            var score = submission.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();

            object? fb = null;
            if (!string.IsNullOrWhiteSpace(score?.AiFeedback))
            {
                try
                {
                    fb = JsonSerializer.Deserialize<object>(score.AiFeedback);
                }
                catch { }
            }

            var dto = new AttemptDetailDto
            {
                SubmissionId = submission.SubmissionId,
                ClassExerciseId = submission.ClassExerciseId,
                ExerciseId = submission.ExerciseId,
                ExerciseTitle = submission.Exercise?.Title ?? "N/A",
                Type = submission.Exercise?.Type ?? "N/A",
                Question = submission.Exercise?.Question ?? "",
                SampleAnswer = submission.Exercise?.SampleAnswer,
                AudioPath = submission.AudioPath,
                CreatedAt = submission.CreatedAt,
                AttemptNumber = submission.AttemptNumber,
                OverallScore = score?.Overall,
                Pronunciation = score?.Pronunciation,
                Grammar = score?.Grammar,
                LexicalResource = score?.LexicalResource,
                Coherence = score?.Coherence,
                Transcript = submission.Transcript ?? "",
                AiFeedback = score?.AiFeedback,
                FeedbackJson = fb,
                ErrorMessage = submission.ErrorMessage,
                Status = submission.Status.ToString()
            };

            return Ok(ApiResponse<AttemptDetailDto>.SuccessResponse(dto));
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatus(int id)
        {
            var studentId = GetStudentId();
            if (studentId == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Không tìm thấy thông tin người dùng."));

            var submission = await _context.Submissions
                .Include(s => s.Scores)
                .FirstOrDefaultAsync(s => s.SubmissionId == id && s.StudentId == studentId.Value);

            if (submission == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài nộp."));

            var score = submission.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Status = submission.Status.ToString(),
                Overall = score?.Overall,
                Pronunciation = score?.Pronunciation,
                Grammar = score?.Grammar,
                LexicalResource = score?.LexicalResource,
                Coherence = score?.Coherence,
                AiFeedback = score?.AiFeedback
            }));
        }
    }
}
