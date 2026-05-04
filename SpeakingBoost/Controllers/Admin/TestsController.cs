using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using ClosedXML.Excel;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "admin")]
    public class TestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/topics
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/topics")]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .OrderBy(t => t.Name)
                .Select(t => new TopicDto
                {
                    TopicId       = t.TopicId,
                    Name          = t.Name,
                    Description   = t.Description,
                    ExerciseCount = t.Exercises != null ? t.Exercises.Count : 0
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TopicDto>>.SuccessResponse(topics));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/tests/topics
        // Body: CreateTopicDto
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
                Description = dto.Description
            };

            _context.VocabularyTopics.Add(topic);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTopicDetails), new { id = topic.TopicId },
                ApiResponse<TopicDto>.SuccessResponse(new TopicDto
                {
                    TopicId     = topic.TopicId,
                    Name        = topic.Name,
                    Description = topic.Description
                }, "Thêm chủ đề thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/tests/topics/{id}
        // Body: CreateTopicDto (reuse same fields: name, description)
        // ────────────────────────────────────────────────────────────
        [HttpPut("api/admin/tests/topics/{id}")]
        public async Task<IActionResult> UpdateTopic(int id, [FromBody] CreateTopicDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var topic = await _context.VocabularyTopics.FindAsync(id);
            if (topic == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            if (await _context.VocabularyTopics.AnyAsync(t => t.Name.ToLower() == dto.Name.ToLower() && t.TopicId != id))
                return Conflict(ApiResponse<object>.ErrorResponse("Tên chủ đề này đã tồn tại."));

            topic.Name        = dto.Name;
            topic.Description = dto.Description;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật chủ đề thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/tests/topics/{id}
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
                    "Không thể xóa chủ đề này vì đã có học viên nộp bài."));

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
                Exercises   = topic.Exercises?.Select(e => new ExerciseDto
                {
                    ExerciseId   = e.ExerciseId,
                    Title        = e.Title,
                    Type         = e.Type,
                    Question     = e.Question,
                    SampleAnswer = e.SampleAnswer,
                    MaxAttempts  = e.MaxAttempts,
                    TopicId      = e.TopicId
                }).ToList() ?? new List<ExerciseDto>()
            };

            return Ok(ApiResponse<TopicDetailsDto>.SuccessResponse(dto));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/tests/topics/{id}/exercises
        // Body: CreateExerciseDto
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/tests/topics/{id}/exercises")]
        public async Task<IActionResult> AddExercise(int id, [FromBody] CreateExerciseDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var exercise = new Exercise
            {
                Title        = dto.Title,
                Type         = dto.Type,
                Question     = dto.Question,
                SampleAnswer = dto.SampleAnswer,
                MaxAttempts  = dto.MaxAttempts,
                TopicId      = id
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
                    TopicId      = exercise.TopicId
                }, "Thêm câu hỏi thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/exercises/{id}
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/exercises/{id}")]
        public async Task<IActionResult> GetExercise(int id)
        {
            var exercise = await _context.Exercises
                .Include(e => e.VocabularyTopic)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

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
                TopicName    = exercise.VocabularyTopic?.Name
            }));
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/tests/exercises/{id}
        // Body: UpdateExerciseDto
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

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật câu hỏi thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/tests/exercises/{id}
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
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/tests/topics/{id}/import")]
        public async Task<IActionResult> ImportFromExcel(int id, IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn file Excel."));

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

                    var title        = excelRow.Cell(1).GetValue<string>()?.Trim();
                    var partVal      = excelRow.Cell(2).GetValue<string>()?.Trim();
                    var questionText = excelRow.Cell(3).GetValue<string>()?.Trim();
                    var sampleAnswer = excelRow.Cell(4).GetValue<string>()?.Trim();
                    var maxVal       = excelRow.Cell(5).GetValue<string>()?.Trim();

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
