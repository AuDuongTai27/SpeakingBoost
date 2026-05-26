using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/deadlines")]
    [Authorize(Roles = "user")]
    public class DeadlineController : ControllerBase
    {
        private readonly IStudentDeadlineService _deadlineService;
        private readonly IServiceProvider _serviceProvider;

        public DeadlineController(IStudentDeadlineService deadlineService, IServiceProvider serviceProvider)
        {
            _deadlineService = deadlineService;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public async Task<IActionResult> GetDeadlines()
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _deadlineService.GetDeadlinesAsync(studentId.Value);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{classExerciseId}")]
        public async Task<IActionResult> GetDeadlineQuestion(int classExerciseId)
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _deadlineService.GetDeadlineQuestionAsync(classExerciseId, studentId.Value);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(IFormFile audio, [FromForm] int exerciseId, [FromForm] int classExerciseId, [FromForm] int part)
        {
            if (audio == null || audio.Length == 0)
                return StatusCode(400, BaseResponse<object>.Fail("File audio không tồn tại.", 400));

            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _deadlineService.SubmitAudioAsync(audio, exerciseId, classExerciseId, part, studentId.Value, _serviceProvider);
            return StatusCode(result.StatusCode, result);
        }
    }
}
