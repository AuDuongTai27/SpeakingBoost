using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using System.Security.Claims;
using ClosedXML.Excel;

// ================================================================
// TestsController — tương đương TestManagementController (MVC)
//
// MVC cũ:                                        API mới:
// ─────────────────────────────────────────────  ──────────────────────────────────────────
// GET  /Admin/TestManagement/Index             →  GET    /api/admin/tests/topics
// POST /Admin/TestManagement/CreateTopic       →  POST   /api/admin/tests/topics
// POST /Admin/TestManagement/DeleteTopic/5     →  DELETE /api/admin/tests/topics/5
// GET  /Admin/TestManagement/TopicDetails/5    →  GET    /api/admin/tests/topics/5
// POST /Admin/TestManagement/AddExercise       →  POST   /api/admin/tests/topics/{id}/exercises
// GET  /Admin/TestManagement/EditExercise/5    →  GET    /api/admin/tests/exercises/5
// POST /Admin/TestManagement/EditExercise/5    →  PUT    /api/admin/tests/exercises/5
// POST /Admin/TestManagement/DeleteExercise    →  DELETE /api/admin/tests/exercises/5
// POST /Admin/TestManagement/ImportQuestions   →  POST   /api/admin/tests/topics/{id}/import
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "teacher,superadmin")]
    public class TestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: lấy UserId admin từ JWT claim
        private int GetAdminUserId()
        {
            var claim = User.FindFirst("StudentId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/topics
        // MVC cũ: Index() → return View(topics)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/topics")]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TopicDto
                {
                    TopicId       = t.TopicId,
                    Name          = t.Name,
                    Description   = t.Description,
                    CreatedAt     = t.CreatedAt,
                    ExerciseCount = t.Exercises != null ? t.Exercises.Count : 0
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TopicDto>>.SuccessResponse(topics));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/tests/topics
        // Body: CreateTopicDto
        // MVC cũ: CreateTopic(model) → _context.Add → RedirectToAction(TopicDetails)
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/tests/topics")]
        public async Task<IActionResult> CreateTopic([FromBody] CreateTopicDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            if (await _context.VocabularyTopics.AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower()))
                return Conflict(ApiResponse<object>.ErrorResponse("Chủ đề này đã tồn tại."));

            var topic = new VocabularyTopic
            {
                Name        = dto.Name,
                Description = dto.Description,
                CreatedAt   = DateTime.Now
            };

            _context.VocabularyTopics.Add(topic);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTopicDetails), new { id = topic.TopicId },
                ApiResponse<TopicDto>.SuccessResponse(new TopicDto
                {
                    TopicId   = topic.TopicId,
                    Name      = topic.Name,
                    Description = topic.Description,
                    CreatedAt = topic.CreatedAt
                }, "Thêm chủ đề thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/tests/topics/{id}
        // MVC cũ: DeleteTopic(id) — xóa topic + exercises nếu chưa có bài nộp
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/tests/topics/{id}")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var topic = await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            var exerciseIds = topic.Exercises?.Select(e => e.ExerciseId).ToList() ?? new List<int>();
            var hasSubmissions = await _context.Submissions.AnyAsync(s => exerciseIds.Contains(s.ExerciseId));

            if (hasSubmissions)
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Không thể xóa chủ đề này vì đã có học sinh nộp bài. Hãy xóa bài nộp trước."));

            try
            {
                if (topic.Exercises != null)
                    _context.Exercises.RemoveRange(topic.Exercises);

                _context.VocabularyTopics.Remove(topic);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi khi xóa: " + ex.Message));
            }
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/topics/{id}
        // MVC cũ: TopicDetails(id) → return View(viewModel)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/topics/{id}")]
        public async Task<IActionResult> GetTopicDetails(int id)
        {
            var topic = await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            var dto = new TopicDetailsDto
            {
                TopicId     = topic.TopicId,
                Name        = topic.Name,
                Description = topic.Description,
                CreatedAt   = topic.CreatedAt,
                Exercises   = topic.Exercises?.Select(e => new ExerciseDto
                {
                    ExerciseId   = e.ExerciseId,
                    Title        = e.Title,
                    Type         = e.Type,
                    Question     = e.Question,
                    SampleAnswer = e.SampleAnswer,
                    MaxAttempts  = e.MaxAttempts,
                    TopicId      = e.TopicId,
                    CreatedAt    = e.CreatedAt
                }).ToList() ?? new List<ExerciseDto>()
            };

            return Ok(ApiResponse<TopicDetailsDto>.SuccessResponse(dto));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/tests/topics/{id}/exercises
        // Body: CreateExerciseDto
        // MVC cũ: AddExercise(newExercise) — gán adminId, TopicId từ form
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/tests/topics/{id}/exercises")]
        public async Task<IActionResult> AddExercise(int id, [FromBody] CreateExerciseDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var adminId = GetAdminUserId();
            if (adminId == 0)
                return Unauthorized(ApiResponse<object>.ErrorResponse("Không xác định được người tạo."));

            var exercise = new Exercise
            {
                Title        = dto.Title,
                Type         = dto.Type,
                Question     = dto.Question,
                SampleAnswer = dto.SampleAnswer,
                MaxAttempts  = dto.MaxAttempts,
                TopicId      = id,
                CreatedBy    = adminId,
                CreatedAt    = DateTime.Now
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExercise), new { id = exercise.ExerciseId },
                ApiResponse<ExerciseDto>.SuccessResponse(new ExerciseDto
                {
                    ExerciseId   = exercise.ExerciseId,
                    Title        = exercise.Title,
                    Type         = exercise.Type,
                    Question     = exercise.Question,
                    SampleAnswer = exercise.SampleAnswer,
                    MaxAttempts  = exercise.MaxAttempts,
                    TopicId      = exercise.TopicId,
                    CreatedAt    = exercise.CreatedAt
                }, "Thêm câu hỏi thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/exercises/{id}
        // MVC cũ: EditExercise(id) → return View(exercise)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/exercises/{id}")]
        public async Task<IActionResult> GetExercise(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy câu hỏi."));

            return Ok(ApiResponse<ExerciseDto>.SuccessResponse(new ExerciseDto
            {
                ExerciseId   = exercise.ExerciseId,
                Title        = exercise.Title,
                Type         = exercise.Type,
                Question     = exercise.Question,
                SampleAnswer = exercise.SampleAnswer,
                MaxAttempts  = exercise.MaxAttempts,
                TopicId      = exercise.TopicId,
                CreatedAt    = exercise.CreatedAt
            }));
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/tests/exercises/{id}
        // Body: UpdateExerciseDto
        // MVC cũ: EditExercise(id, exercise) → _context.Update → RedirectToAction(TopicDetails)
        // ────────────────────────────────────────────────────────────
        [HttpPut("api/admin/tests/exercises/{id}")]
        public async Task<IActionResult> UpdateExercise(int id, [FromBody] UpdateExerciseDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy câu hỏi."));

            exercise.Title        = dto.Title;
            exercise.Type         = dto.Type;
            exercise.Question     = dto.Question;
            exercise.SampleAnswer = dto.SampleAnswer;
            exercise.MaxAttempts  = dto.MaxAttempts;
            exercise.TopicId      = dto.TopicId;
            // Không cập nhật CreatedAt, CreatedBy — giống MVC

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật câu hỏi thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/tests/exercises/{id}
        // MVC cũ: DeleteExercise(exerciseId) — xóa submissions trước, rồi xóa exercise
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/tests/exercises/{id}")]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            var exercise = await _context.Exercises
                .Include(e => e.Submissions)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy câu hỏi."));

            try
            {
                if (exercise.Submissions != null && exercise.Submissions.Any())
                    _context.Submissions.RemoveRange(exercise.Submissions);

                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi khi xóa: " + ex.Message));
            }
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/tests/topics/{id}/import
        // Form: excelFile (IFormFile)
        // MVC cũ: ImportQuestionsFromExcel(topicId, excelFile) — đọc ClosedXML
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/tests/topics/{id}/import")]
        public async Task<IActionResult> ImportFromExcel(int id, IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn file Excel."));

            var adminId = GetAdminUserId();

            int successCount = 0;
            int currentRow = 2;

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var lastRow   = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                for (int row = 2; row <= lastRow; row++)
                {
                    currentRow = row;
                    var excelRow = worksheet.Row(row);

                    var title       = excelRow.Cell(1).GetValue<string>()?.Trim();
                    var partVal     = excelRow.Cell(2).GetValue<string>()?.Trim();
                    var questionText = excelRow.Cell(3).GetValue<string>()?.Trim();
                    var sampleAnswer = excelRow.Cell(4).GetValue<string>()?.Trim();
                    var maxVal      = excelRow.Cell(5).GetValue<string>()?.Trim();

                    if (string.IsNullOrEmpty(questionText)) continue;

                    int.TryParse(partVal, out int partNumber);
                    string typeString = partNumber switch { 2 => "Part2", 3 => "Part3", _ => "Part1" };

                    if (!int.TryParse(maxVal, out int maxAttempts) || maxAttempts <= 0)
                        maxAttempts = 3;

                    _context.Exercises.Add(new Exercise
                    {
                        TopicId      = id,
                        Title        = string.IsNullOrEmpty(title) ? $"Câu hỏi (Dòng {row})" : title,
                        Type         = typeString,
                        Question     = questionText,
                        SampleAnswer = sampleAnswer,
                        CreatedBy    = adminId,
                        CreatedAt    = DateTime.Now,
                        MaxAttempts  = maxAttempts
                    });
                    successCount++;
                }

                if (successCount > 0)
                    await _context.SaveChangesAsync();
                else
                    return BadRequest(ApiResponse<object>.ErrorResponse("File không có dữ liệu hợp lệ."));

                return Ok(ApiResponse<object>.SuccessResponse($"Đã nhập thành công {successCount} câu hỏi!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi tại dòng {currentRow}: {ex.Message}"));
            }
        }
    }
}
