using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakingBoost.Helpers;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "admin")]
    public class ClassesController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassesController(IClassService classService)
        {
            _classService = classService;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/classes
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/classes")]
        public async Task<IActionResult> GetAllClasses()
        {
            var classes = await _classService.GetAllClassesAsync();
            return Ok(BaseResponse<List<ClassDto>>.Ok(classes));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/classes/{id}
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/classes/{id}")]
        public async Task<IActionResult> GetClass(int id)
        {
            try
            {
                var schoolClass = await _classService.GetClassByIdAsync(id);
                return Ok(BaseResponse<ClassDto>.Ok(schoolClass));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/classes
        // Body: CreateClassDto
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/classes")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                var createdClass = await _classService.CreateClassAsync(dto);
                return CreatedAtAction(nameof(GetClass), new { id = createdClass.ClassId },
                    BaseResponse<ClassDto>.Ok(createdClass, "Tạo lớp thành công!"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(BaseResponse<object>.Fail(ex.Message, 409));
            }
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/classes/{id}
        // Body: UpdateClassDto
        // ────────────────────────────────────────────────────────────
        [HttpPut("api/admin/classes/{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] UpdateClassDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(BaseResponse<object>.Fail("Dữ liệu không hợp lệ: " + string.Join(", ", errors), 400));
            }

            try
            {
                await _classService.UpdateClassAsync(id, dto);
                return Ok(BaseResponse<object>.Ok("Cập nhật lớp thành công!"));
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
        // DELETE /api/admin/classes/{id}
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/classes/{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                await _classService.DeleteClassAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Fail("Không thể xóa lớp: " + ex.Message, 500));
            }
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/classes/{id}/details
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/classes/{id}/details")]
        public async Task<IActionResult> GetClassDetails(int id)
        {
            try
            {
                var details = await _classService.GetClassDetailsAsync(id);
                return Ok(BaseResponse<ClassDetailsDto>.Ok(details));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/classes/{id}/students
        // Body: AddStudentToClassDto
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/classes/{id}/students")]
        public async Task<IActionResult> AddStudentToClass(int id, [FromBody] AddStudentToClassDto dto)
        {
            try
            {
                await _classService.AddStudentToClassAsync(id, dto);
                return Ok(BaseResponse<object>.Ok("Thêm học viên vào lớp thành công!"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(BaseResponse<object>.Fail(ex.Message, 409));
            }
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/classes/{id}/students/{studentClassId}
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/classes/{id}/students/{studentClassId}")]
        public async Task<IActionResult> RemoveStudentFromClass(int id, int studentClassId)
        {
            try
            {
                await _classService.RemoveStudentFromClassAsync(id, studentClassId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }

        // ────────────────────────────────────────────────────────────
        // PATCH /api/admin/classes/exercises/{classExerciseId}/deadline
        // Body: UpdateDeadlineInClassDto
        // ────────────────────────────────────────────────────────────
        [HttpPatch("api/admin/classes/exercises/{classExerciseId}/deadline")]
        public async Task<IActionResult> UpdateDeadline(int classExerciseId, [FromBody] UpdateDeadlineInClassDto dto)
        {
            try
            {
                await _classService.UpdateDeadlineAsync(classExerciseId, dto);
                return Ok(BaseResponse<object>.Ok("Cập nhật deadline thành công!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(BaseResponse<object>.Fail(ex.Message, 404));
            }
        }
    }
}
