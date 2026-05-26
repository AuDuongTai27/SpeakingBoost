using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "admin")]
    public class DeadlinesController : ControllerBase
    {
        private readonly IDeadlineService _deadlineService;

        public DeadlinesController(IDeadlineService deadlineService)
        {
            _deadlineService = deadlineService;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/deadlines
        // Trả danh sách deadline đang chạy + dữ liệu dropdown cho frontend
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/deadlines")]
        public async Task<IActionResult> GetActiveDeadlines()
        {
            var data = await _deadlineService.GetActiveDeadlinesDataAsync();
            return Ok(BaseResponse<object>.Ok(data));
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
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                var message = await _deadlineService.AssignTopicDeadlineAsync(dto);
                return Ok(BaseResponse<object>.Ok(null, message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(BaseResponse<object>.Fail(ex.Message, 400));
            }
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/deadlines/{id}
        // Xóa 1 ClassExercise (1 câu hỏi khỏi deadline của lớp)
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/deadlines/{id}")]
        public async Task<IActionResult> DeleteDeadline(int id)
        {
            try
            {
                await _deadlineService.DeleteDeadlineAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/deadlines/topic/{topicId}/class/{classId}
        // Xóa toàn bộ deadline của 1 topic khỏi 1 lớp
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/deadlines/topic/{topicId}/class/{classId}")]
        public async Task<IActionResult> DeleteTopicDeadlineFromClass(int topicId, int classId)
        {
            try
            {
                await _deadlineService.DeleteTopicDeadlineFromClassAsync(topicId, classId);
                return Ok(BaseResponse<object>.Ok("Đã xóa toàn bộ deadline của chủ đề khỏi lớp."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }
    }
}
