using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "admin")]
    public class StudentsAdminController : ControllerBase
    {
        private readonly IStudentsAdminService _studentsAdminService;

        public StudentsAdminController(IStudentsAdminService studentsAdminService)
        {
            _studentsAdminService = studentsAdminService;
        }

        // GET /api/admin/students — tổng quan deadline từng học viên
        [HttpGet("api/admin/students")]
        public async Task<IActionResult> GetStudentsSummary()
        {
            var summary = await _studentsAdminService.GetStudentsSummaryAsync();
            return Ok(BaseResponse<List<StudentSummaryDto>>.Ok(summary));
        }

        // GET /api/admin/students/{id}/details
        [HttpGet("api/admin/students/{id}/details")]
        public async Task<IActionResult> GetStudentDetails(int id)
        {
            try
            {
                var details = await _studentsAdminService.GetStudentDetailsAsync(id);
                return Ok(BaseResponse<StudentDetailsDto>.Ok(details));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // GET /api/admin/students/{studentId}/exercises/{exerciseId}/history
        [HttpGet("api/admin/students/{studentId}/exercises/{exerciseId}/history")]
        public async Task<IActionResult> GetHistory(int studentId, int exerciseId)
        {
            try
            {
                var history = await _studentsAdminService.GetAttemptHistoryAsync(studentId, exerciseId);
                return Ok(BaseResponse<object>.Ok(history));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // GET /api/admin/submissions/{id}
        [HttpGet("api/admin/submissions/{id}")]
        public async Task<IActionResult> GetSubmissionDetail(int id)
        {
            try
            {
                var submission = await _studentsAdminService.GetSubmissionDetailAsync(id);
                return Ok(BaseResponse<AttemptDetailAdminDto>.Ok(submission));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }
    }
}
