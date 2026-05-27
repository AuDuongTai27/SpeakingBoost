using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "admin")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _dashboardService;

        public AdminDashboardController(IAdminDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/dashboard?classId=5
        // Trả số liệu dashboard cho lớp được chọn
        // ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int? classId = null)
        {
            try
            {
                var data = await _dashboardService.GetDashboardDataAsync(classId);
                return Ok(BaseResponse<AdminDashboardDto>.Ok(data));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Lỗi hệ thống: " + ex.Message, 500));
            }
        }
    }
}
