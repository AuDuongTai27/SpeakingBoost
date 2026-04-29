using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;

// ================================================================
// ClassesController — tương đương StudentManagementController (MVC)
//
// MVC cũ:                                    API mới:
// ─────────────────────────────────────────  ──────────────────────────────────────────────
// GET  /Admin/StudentManagement/Index      →  GET    /api/admin/classes
// POST /Admin/StudentManagement/CreateClass→  POST   /api/admin/classes
// GET  /Admin/StudentManagement/Edit/5     →  GET    /api/admin/classes/5
// POST /Admin/StudentManagement/Edit/5     →  PUT    /api/admin/classes/5
// POST /Admin/StudentManagement/DeleteClass→  DELETE /api/admin/classes/5
// GET  /Admin/StudentManagement/ClassDetails/5  →  GET /api/admin/classes/5/details
// POST /Admin/StudentManagement/AddStudentToClass    →  POST   /api/admin/classes/5/students
// POST /Admin/StudentManagement/RemoveStudentFromClass → DELETE /api/admin/classes/5/students/{studentClassId}
// POST /Admin/StudentManagement/AssignExercise       →  POST   /api/admin/classes/5/exercises
// POST /Admin/StudentManagement/RemoveExerciseFromClass → DELETE /api/admin/classes/5/exercises/{classExerciseId}
// POST /Admin/StudentManagement/UpdateExerciseDeadline  → PATCH  /api/admin/classes/exercises/{classExerciseId}/deadline
// GET  /Admin/StudentManagement/StudentDetails/5     →  GET    /api/admin/students/5/details
// GET  /Admin/StudentManagement/ViewStudents         →  GET    /api/admin/students
// GET  /Admin/StudentManagement/History              →  GET    /api/admin/students/{studentId}/exercises/{exerciseId}/history
// GET  /Admin/StudentManagement/AttemptDetail/5      →  GET    /api/admin/submissions/5
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "teacher,superadmin")]
    public class ClassesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClassesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/classes
        // MVC cũ: Index() → return View(classes)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/classes")]
        public async Task<IActionResult> GetAllClasses()
        {
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.StudentClasses)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClassDto
                {
                    ClassId      = c.ClassId,
                    ClassName    = c.ClassName,
                    TeacherId    = c.TeacherId,
                    TeacherName  = c.Teacher != null ? c.Teacher.FullName : null,
                    CreatedAt    = c.CreatedAt,
                    StudentCount = c.StudentClasses.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ClassDto>>.SuccessResponse(classes));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/classes/{id}
        // MVC cũ: Edit(id) → return View(schoolClass)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/classes/{id}")]
        public async Task<IActionResult> GetClass(int id)
        {
            var c = await _context.Classes
                .Include(x => x.Teacher)
                .Include(x => x.StudentClasses)
                .FirstOrDefaultAsync(x => x.ClassId == id);

            if (c == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy lớp học."));

            return Ok(ApiResponse<ClassDto>.SuccessResponse(new ClassDto
            {
                ClassId      = c.ClassId,
                ClassName    = c.ClassName,
                TeacherId    = c.TeacherId,
                TeacherName  = c.Teacher?.FullName,
                CreatedAt    = c.CreatedAt,
                StudentCount = c.StudentClasses.Count
            }));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/classes
        // Body: CreateClassDto
        // MVC cũ: CreateClass(model) → _context.Add → RedirectToAction
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/classes")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            if (await _context.Classes.AnyAsync(c => c.ClassName == dto.ClassName))
                return Conflict(ApiResponse<object>.ErrorResponse("Tên lớp này đã tồn tại."));

            var schoolClass = new SchoolClass
            {
                ClassName = dto.ClassName,
                TeacherId = dto.TeacherId,
                CreatedAt = DateTime.Now
            };

            _context.Classes.Add(schoolClass);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClass), new { id = schoolClass.ClassId },
                ApiResponse<ClassDto>.SuccessResponse(new ClassDto
                {
                    ClassId   = schoolClass.ClassId,
                    ClassName = schoolClass.ClassName,
                    TeacherId = schoolClass.TeacherId,
                    CreatedAt = schoolClass.CreatedAt
                }, "Tạo lớp thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // PUT /api/admin/classes/{id}
        // Body: UpdateClassDto
        // MVC cũ: Edit(id, schoolClass) → _context.Update → RedirectToAction
        // ────────────────────────────────────────────────────────────
        [HttpPut("api/admin/classes/{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] UpdateClassDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            var schoolClass = await _context.Classes.FindAsync(id);
            if (schoolClass == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy lớp học."));

            if (await _context.Classes.AnyAsync(c => c.ClassName == dto.ClassName && c.ClassId != id))
                return Conflict(ApiResponse<object>.ErrorResponse("Tên lớp này đã tồn tại."));

            schoolClass.ClassName = dto.ClassName;
            schoolClass.TeacherId = dto.TeacherId;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật lớp thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/classes/{id}
        // MVC cũ: DeleteClass(id) → _context.Remove → RedirectToAction
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/classes/{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var schoolClass = await _context.Classes.FindAsync(id);
            if (schoolClass == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy lớp học."));

            try
            {
                _context.Classes.Remove(schoolClass);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Không thể xóa lớp: " + ex.Message));
            }
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/classes/{id}/details
        // MVC cũ: ClassDetails(id) → return View(viewModel)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/classes/{id}/details")]
        public async Task<IActionResult> GetClassDetails(int id)
        {
            var schoolClass = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.StudentClasses)
                    .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (schoolClass == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy lớp học."));

            var assignedExercises = await _context.ClassExercises
                .Include(ce => ce.Exercise)
                .Where(ce => ce.ClassId == id)
                .OrderBy(ce => ce.Deadline)
                .ToListAsync();

            var studentIds = schoolClass.StudentClasses.Select(sc => sc.StudentId).ToList();
            var submissionCounts = await _context.Submissions
                .Where(s => studentIds.Contains(s.StudentId))
                .GroupBy(s => s.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StudentId, x => x.Count);

            var dto = new ClassDetailsDto
            {
                ClassId   = schoolClass.ClassId,
                ClassName = schoolClass.ClassName,
                TeacherName = schoolClass.Teacher?.FullName,
                Students = schoolClass.StudentClasses.Select(sc => new StudentInClassDto
                {
                    StudentClassId  = sc.StudentClassId,
                    StudentId       = sc.StudentId,
                    FullName        = sc.Student.FullName,
                    Email           = sc.Student.Email,
                    SubmissionCount = submissionCounts.GetValueOrDefault(sc.StudentId, 0)
                }).ToList(),
                AssignedExercises = assignedExercises.Select(ce => new AssignedExerciseDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ExerciseId      = ce.ExerciseId,
                    Title           = ce.Exercise.Title,
                    Type            = ce.Exercise.Type,
                    Deadline        = ce.Deadline
                }).ToList()
            };

            return Ok(ApiResponse<ClassDetailsDto>.SuccessResponse(dto));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/classes/{id}/students
        // Body: AddStudentToClassDto
        // MVC cũ: AddStudentToClass(classId, studentId)
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/classes/{id}/students")]
        public async Task<IActionResult> AddStudentToClass(int id, [FromBody] AddStudentToClassDto dto)
        {
            var exists = await _context.StudentClasses
                .AnyAsync(sc => sc.ClassId == id && sc.StudentId == dto.StudentId);

            if (exists)
                return Conflict(ApiResponse<object>.ErrorResponse("Sinh viên đã có trong lớp này."));

            _context.StudentClasses.Add(new StudentClass { ClassId = id, StudentId = dto.StudentId });
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Thêm sinh viên vào lớp thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/classes/{id}/students/{studentClassId}
        // MVC cũ: RemoveStudentFromClass(studentClassId)
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/classes/{id}/students/{studentClassId}")]
        public async Task<IActionResult> RemoveStudentFromClass(int id, int studentClassId)
        {
            var record = await _context.StudentClasses.FindAsync(studentClassId);
            if (record == null || record.ClassId != id)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bản ghi."));

            _context.StudentClasses.Remove(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/classes/{id}/exercises
        // Body: AssignExerciseDto
        // MVC cũ: AssignExercise(classId, exerciseId, deadline?)
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/classes/{id}/exercises")]
        public async Task<IActionResult> AssignExercise(int id, [FromBody] AssignExerciseDto dto)
        {
            var exists = await _context.ClassExercises
                .AnyAsync(ce => ce.ClassId == id && ce.ExerciseId == dto.ExerciseId);

            if (exists)
                return Conflict(ApiResponse<object>.ErrorResponse("Bài tập này đã được gán cho lớp."));

            _context.ClassExercises.Add(new ClassExercise
            {
                ClassId    = id,
                ExerciseId = dto.ExerciseId,
                Deadline   = dto.Deadline
            });
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Gán bài tập thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/classes/{id}/exercises/{classExerciseId}
        // MVC cũ: RemoveExerciseFromClass(classExerciseId)
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/classes/{id}/exercises/{classExerciseId}")]
        public async Task<IActionResult> RemoveExerciseFromClass(int id, int classExerciseId)
        {
            var assignment = await _context.ClassExercises.FindAsync(classExerciseId);
            if (assignment == null || assignment.ClassId != id)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài tập được gán."));

            _context.ClassExercises.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ────────────────────────────────────────────────────────────
        // PATCH /api/admin/classes/exercises/{classExerciseId}/deadline
        // Body: UpdateDeadlineInClassDto
        // MVC cũ: UpdateExerciseDeadline(classExerciseId, newDeadline?)
        // ────────────────────────────────────────────────────────────
        [HttpPatch("api/admin/classes/exercises/{classExerciseId}/deadline")]
        public async Task<IActionResult> UpdateDeadline(int classExerciseId, [FromBody] UpdateDeadlineInClassDto dto)
        {
            var assignment = await _context.ClassExercises.FindAsync(classExerciseId);
            if (assignment == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài tập được gán."));

            assignment.Deadline = dto.Deadline;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Cập nhật deadline thành công!"));
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Students sub-resource (vẫn trong namespace Admin)
    // ────────────────────────────────────────────────────────────────
    [ApiController]
    [Authorize(Roles = "teacher,superadmin")]
    public class StudentsAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentsAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/students
        // MVC cũ: ViewStudents() — tổng quan deadline từng học sinh
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/students")]
        public async Task<IActionResult> GetStudentsSummary()
        {
            var students = await _context.Users
                .Where(u => u.Role == "Student")
                .Include(u => u.Submissions)
                .Include(u => u.StudentClasses)
                .ToListAsync();

            var result = new List<StudentSummaryDto>();

            foreach (var student in students)
            {
                var classIds = student.StudentClasses.Select(sc => sc.ClassId).ToList();

                var assignedExercises = await _context.ClassExercises
                    .Where(ce => classIds.Contains(ce.ClassId) && ce.Deadline.HasValue)
                    .ToListAsync();

                int submitted = student.Submissions
                    .Select(s => s.ExerciseId)
                    .Distinct()
                    .Count(exId => assignedExercises.Any(ae => ae.ExerciseId == exId));

                int late = 0;
                foreach (var sub in student.Submissions)
                {
                    var assignment = assignedExercises.FirstOrDefault(ae => ae.ExerciseId == sub.ExerciseId);
                    if (assignment != null && sub.CreatedAt > assignment.Deadline)
                        late++;
                }

                int missing = assignedExercises
                    .Where(ae => ae.Deadline < DateTime.Now)
                    .Count(ae => !student.Submissions.Any(s => s.ExerciseId == ae.ExerciseId));

                result.Add(new StudentSummaryDto
                {
                    StudentId       = student.UserId,
                    StudentName     = student.FullName,
                    Email           = student.Email,
                    SubmittedCount  = submitted,
                    SubmittedLateCount = late,
                    MissingCount    = missing
                });
            }

            return Ok(ApiResponse<List<StudentSummaryDto>>.SuccessResponse(result));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/students/{id}/details
        // MVC cũ: StudentDetails(id) — xem chi tiết 1 học sinh
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/students/{id}/details")]
        public async Task<IActionResult> GetStudentDetails(int id)
        {
            var student = await _context.Users
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Exercise)
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Scores)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Student");

            if (student == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy học sinh."));

            var chartData = student.Submissions
                .Where(s => s.Scores.Any())
                .OrderBy(s => s.CreatedAt)
                .Select(s => new
                {
                    Date  = s.CreatedAt.ToString("dd/MM"),
                    Score = s.Scores.OrderByDescending(sc => sc.CreatedAt).First().Overall ?? 0
                }).ToList();

            var dto = new StudentDetailsDto
            {
                UserId   = student.UserId,
                FullName = student.FullName,
                Email    = student.Email,
                ChartLabels = chartData.Select(d => d.Date).ToList(),
                ChartValues = chartData.Select(d => d.Score).ToList(),
                Submissions = student.Submissions
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new SubmissionSummaryDto
                    {
                        SubmissionId   = s.SubmissionId,
                        ExerciseId     = s.ExerciseId,
                        ExerciseTitle  = s.Exercise?.Title ?? "",
                        CreatedAt      = s.CreatedAt,
                        Overall        = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault()?.Overall,
                        Status         = s.Status.ToString()
                    }).ToList()
            };

            return Ok(ApiResponse<StudentDetailsDto>.SuccessResponse(dto));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/students/{studentId}/exercises/{exerciseId}/history
        // MVC cũ: History(exerciseId, studentId)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/students/{studentId}/exercises/{exerciseId}/history")]
        public async Task<IActionResult> GetHistory(int studentId, int exerciseId)
        {
            var student  = await _context.Users.FindAsync(studentId);
            var exercise = await _context.Exercises.FindAsync(exerciseId);

            if (student == null || exercise == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy học sinh hoặc bài tập."));

            var submissions = await _context.Submissions
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId && s.ExerciseId == exerciseId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var history = submissions.Select(s =>
            {
                var latest = s.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();
                return new AttemptHistoryAdminDto
                {
                    SubmissionId  = s.SubmissionId,
                    AttemptNumber = s.AttemptNumber,
                    CreatedAt     = s.CreatedAt,
                    Overall       = latest?.Overall,
                    Status        = s.Status.ToString(),
                    ErrorMessage  = s.ErrorMessage
                };
            }).ToList();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                StudentName   = student.FullName,
                ExerciseTitle = exercise.Title,
                Items         = history
            }));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/submissions/{id}
        // MVC cũ: AttemptDetail(id)
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/submissions/{id}")]
        public async Task<IActionResult> GetSubmissionDetail(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Student)
                .Include(s => s.Scores)
                .FirstOrDefaultAsync(s => s.SubmissionId == id);

            if (submission == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy bài nộp."));

            var score = submission.Scores.OrderByDescending(sc => sc.CreatedAt).FirstOrDefault();

            var dto = new AttemptDetailAdminDto
            {
                SubmissionId   = submission.SubmissionId,
                StudentName    = submission.Student?.FullName ?? "",
                ExerciseTitle  = submission.Exercise?.Title ?? "",
                AudioPath      = submission.AudioPath,
                Transcript     = submission.Transcript,
                CreatedAt      = submission.CreatedAt,
                Overall        = score?.Overall,
                Pronunciation  = score?.Pronunciation,
                Grammar        = score?.Grammar,
                LexicalResource = score?.LexicalResource,
                Coherence      = score?.Coherence,
                AiFeedback     = score?.AiFeedback
            };

            return Ok(ApiResponse<AttemptDetailAdminDto>.SuccessResponse(dto));
        }
    }
}
