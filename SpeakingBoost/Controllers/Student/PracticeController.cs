using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/practice")]
    [Authorize(Roles = "user")]
    public class PracticeController : ControllerBase
    {
        private readonly IPracticeService _practiceService;
        private readonly IServiceProvider _serviceProvider;

        public PracticeController(IPracticeService practiceService, IServiceProvider serviceProvider)
        {
            _practiceService = practiceService;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics([FromQuery] int part = 0)
        {
            var result = await _practiceService.GetTopicsAsync(part);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("topics/{id}")]
        public async Task<IActionResult> GetTopicQuestions(int id, [FromQuery] int part = 0)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _practiceService.GetTopicQuestionsAsync(id, part, studentId.Value);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(IFormFile audio, [FromForm] int exerciseId, [FromForm] int part)
        {
            if (audio == null || audio.Length == 0)
                return StatusCode(400, BaseResponse<object>.Fail("File audio không tồn tại.", 400));

            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _practiceService.SubmitAudioAsync(audio, exerciseId, part, studentId.Value, _serviceProvider);
            return StatusCode(result.StatusCode, result);
        }
    }
}
