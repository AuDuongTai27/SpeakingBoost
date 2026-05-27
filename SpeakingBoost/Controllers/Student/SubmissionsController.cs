using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/submissions")]
    [Authorize(Roles = "user")]
    public class SubmissionsController : ControllerBase
    {
        private readonly IStudentSubmissionService _submissionService;

        public SubmissionsController(IStudentSubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        [HttpGet("all-history")]
        public async Task<IActionResult> GetAllHistory()
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _submissionService.GetAllHistoryAsync(studentId.Value);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("practice-history")]
        public async Task<IActionResult> GetHistory([FromQuery] int exerciseId)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _submissionService.GetPracticeHistoryAsync(studentId.Value, exerciseId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("deadline-history")]
        public async Task<IActionResult> GetDeadlineHistory([FromQuery] int classExerciseId)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _submissionService.GetDeadlineHistoryAsync(studentId.Value, classExerciseId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAttemptDetail(int id)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _submissionService.GetAttemptDetailAsync(id, studentId.Value);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatus(int id)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _submissionService.GetStatusAsync(id, studentId.Value);
            return StatusCode(result.StatusCode, result);
        }
    }
}
