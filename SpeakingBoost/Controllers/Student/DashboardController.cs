using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Services.Interfaces.Student;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/dashboard")]
    [Authorize(Roles = "user")]
    public class DashboardController : ControllerBase
    {
        private readonly IStudentDashboardService _dashboardService;

        public DashboardController(IStudentDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var studentId = User.GetStudentId();
            if (studentId == null)
                return StatusCode(401, BaseResponse<object>.Fail("Không tìm thấy thông tin người dùng.", 401));

            var result = await _dashboardService.GetDashboardAsync(studentId.Value);
            return StatusCode(result.StatusCode, result);
        }
    }
}
