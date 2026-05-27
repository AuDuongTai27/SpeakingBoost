using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "admin")]
    public class TestsController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public TestsController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/topics
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/topics")]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _exerciseService.GetAllTopicsAsync();
            return Ok(BaseResponse<List<TopicDto>>.Ok(topics));
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
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                var createdTopic = await _exerciseService.CreateTopicAsync(dto);
                return CreatedAtAction(nameof(GetTopicDetails), new { id = createdTopic.TopicId },
                    BaseResponse<TopicDto>.Ok(createdTopic, "Thêm chủ đề thành công!"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(BaseResponse<object>.Fail(ex.Message, 409));
            }
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/tests/topics/{id}
        // Body: CreateTopicDto
        // ────────────────────────────────────────────────────────────
        [HttpPut("api/admin/tests/topics/{id}")]
        public async Task<IActionResult> UpdateTopic(int id, [FromBody] CreateTopicDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                await _exerciseService.UpdateTopicAsync(id, dto);
                return Ok(BaseResponse<object>.Ok("Cập nhật chủ đề thành công!"));
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
        // DELETE /api/admin/tests/topics/{id}
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/tests/topics/{id}")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            try
            {
                await _exerciseService.DeleteTopicAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(BaseResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi khi xóa: " + ex.Message, 500));
            }
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/topics/{id}
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/topics/{id}")]
        public async Task<IActionResult> GetTopicDetails(int id)
        {
            try
            {
                var details = await _exerciseService.GetTopicDetailsAsync(id);
                return Ok(BaseResponse<TopicDetailsDto>.Ok(details));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
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
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                var createdExercise = await _exerciseService.AddExerciseAsync(id, dto);
                return CreatedAtAction(nameof(GetExercise), new { id = createdExercise.ExerciseId },
                    BaseResponse<ExerciseDto>.Ok(createdExercise, "Thêm câu hỏi thành công!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/tests/exercises/{id}
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/tests/exercises/{id}")]
        public async Task<IActionResult> GetExercise(int id)
        {
            try
            {
                var exercise = await _exerciseService.GetExerciseAsync(id);
                return Ok(BaseResponse<ExerciseDto>.Ok(exercise));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
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
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                await _exerciseService.UpdateExerciseAsync(id, dto);
                return Ok(BaseResponse<object>.Ok("Cập nhật câu hỏi thành công!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/tests/exercises/{id}
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/tests/exercises/{id}")]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            try
            {
                await _exerciseService.DeleteExerciseAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi khi xóa: " + ex.Message, 500));
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
            {
                return BadRequest(BaseResponse<object>.Fail("Vui lòng chọn file Excel.", 400));
            }

            try
            {
                using var stream = excelFile.OpenReadStream();
                int successCount = await _exerciseService.ImportFromExcelAsync(id, stream);
                return Ok(BaseResponse<object>.Ok(null, $"Đã nhập thành công {successCount} câu hỏi!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(BaseResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi nhập dữ liệu: " + ex.Message, 500));
            }
        }
    }
}
